import * as cdk from 'aws-cdk-lib';
import * as lambda from 'aws-cdk-lib/aws-lambda';
import * as apigwv2 from 'aws-cdk-lib/aws-apigatewayv2';
import * as integrations from 'aws-cdk-lib/aws-apigatewayv2-integrations';
import * as sqs from 'aws-cdk-lib/aws-sqs';
import * as events from 'aws-cdk-lib/aws-events';
import * as targets from 'aws-cdk-lib/aws-events-targets';
import * as elasticache from 'aws-cdk-lib/aws-elasticache';
import * as ec2 from 'aws-cdk-lib/aws-ec2';
import * as secretsmanager from 'aws-cdk-lib/aws-secretsmanager';
import { SqsEventSource } from 'aws-cdk-lib/aws-lambda-event-sources';
import { Construct } from 'constructs';
import * as path from 'path';

export class CashFlowStack extends cdk.Stack {
  constructor(scope: Construct, id: string, props?: cdk.StackProps) {
    super(scope, id, props);

    // ── VPC ────────────────────────────────────────────────────────────────────
    const vpc = new ec2.Vpc(this, 'CashFlowVpc', {
      maxAzs: 2,
      natGateways: 1,
    });

    // ── Secrets ────────────────────────────────────────────────────────────────
    const dbSecret = new secretsmanager.Secret(this, 'DbSecret', {
      secretName: 'desafio/db-connection',
      description: 'SQL Server connection string for LancamentosDb',
    });

    const cognitoTokenSecret = new secretsmanager.Secret(this, 'CognitoTokenSecret', {
      secretName: 'desafio/cognito-token',
      description: 'Mock Cognito valid token for local/dev auth',
    });

    // ── SQS Queues ─────────────────────────────────────────────────────────────
    const dlq = new sqs.Queue(this, 'LancamentosDLQ', {
      queueName: 'desafio-lancamentos-criados-dlq',
      retentionPeriod: cdk.Duration.days(7),
    });

    const lancamentosQueue = new sqs.Queue(this, 'LancamentosQueue', {
      queueName: 'desafio-lancamentos-criados',
      visibilityTimeout: cdk.Duration.seconds(60),
      retentionPeriod: cdk.Duration.days(1),
      deadLetterQueue: { queue: dlq, maxReceiveCount: 3 },
    });

    // ── ElastiCache (Redis) ────────────────────────────────────────────────────
    const cacheSubnetGroup = new elasticache.CfnSubnetGroup(this, 'CacheSubnetGroup', {
      description: 'Subnet group for ElastiCache Redis',
      subnetIds: vpc.privateSubnets.map(s => s.subnetId),
    });

    const cacheSg = new ec2.SecurityGroup(this, 'CacheSecurityGroup', {
      vpc,
      description: 'ElastiCache Redis security group',
    });

    const redisCluster = new elasticache.CfnReplicationGroup(this, 'RedisCluster', {
      replicationGroupDescription: 'Desafio cash flow cache',
      numCacheClusters: 2,
      cacheNodeType: 'cache.t4g.micro',
      engine: 'redis',
      engineVersion: '7.1',
      cacheSubnetGroupName: cacheSubnetGroup.ref,
      securityGroupIds: [cacheSg.securityGroupId],
      automaticFailoverEnabled: true,
    });

    // ── Lambda: Lancamentos API ────────────────────────────────────────────────
    const apiFunction = new lambda.Function(this, 'CashFlowApiFunction', {
      functionName: 'desafio-cashflow-api',
      description: 'Cash Flow Lancamentos API — Vertical Slice handlers',
      runtime: lambda.Runtime.DOTNET_10 as unknown as lambda.Runtime,
      code: lambda.Code.fromAsset(
        path.join(__dirname, '../../../../Desafio.Functions.Aws/bin/Release/net10.0/publish')
      ),
      handler: 'Desafio.Functions.Aws::Desafio.Functions.Aws.LambdaEntryPoint::FunctionHandlerAsync',
      timeout: cdk.Duration.seconds(30),
      memorySize: 512,
      vpc,
      environment: {
        ASPNETCORE_ENVIRONMENT: 'Production',
        'ConnectionStrings__cache': `${redisCluster.attrPrimaryEndPointAddress}:${redisCluster.attrPrimaryEndPointPort}`,
        'Aws__Sqs__DefaultQueueUrl': lancamentosQueue.queueUrl,
      },
    });

    dbSecret.grantRead(apiFunction);
    cognitoTokenSecret.grantRead(apiFunction);
    lancamentosQueue.grantSendMessages(apiFunction);

    // ── API Gateway HTTP API ───────────────────────────────────────────────────
    const httpApi = new apigwv2.HttpApi(this, 'CashFlowHttpApi', {
      apiName: 'desafio-cashflow',
      description: 'Cash Flow API Gateway',
      corsPreflight: {
        allowOrigins: ['*'],
        allowMethods: [apigwv2.CorsHttpMethod.ANY],
        allowHeaders: ['Content-Type', 'Authorization', 'x-api-key'],
      },
    });

    httpApi.addRoutes({
      path: '/{proxy+}',
      methods: [apigwv2.HttpMethod.ANY],
      integration: new integrations.HttpLambdaIntegration('ApiIntegration', apiFunction),
    });

    // ── Lambda: Consolidado Worker ────────────────────────────────────────────
    const workerFunction = new lambda.Function(this, 'ConsolidadoWorkerFunction', {
      functionName: 'desafio-consolidado-worker',
      description: 'Consolidado Worker — recalculates daily cash flow summaries',
      runtime: lambda.Runtime.DOTNET_10 as unknown as lambda.Runtime,
      code: lambda.Code.fromAsset(
        path.join(__dirname, '../../../../Desafio.Consolidado.Worker/bin/Release/net10.0/publish')
      ),
      handler: 'Desafio.Consolidado.Worker::Desafio.Consolidado.Worker.LambdaBootstrap::Handler',
      timeout: cdk.Duration.seconds(60),
      memorySize: 256,
      vpc,
      environment: {
        ASPNETCORE_ENVIRONMENT: 'Production',
        'ConnectionStrings__cache': `${redisCluster.attrPrimaryEndPointAddress}:${redisCluster.attrPrimaryEndPointPort}`,
      },
    });

    dbSecret.grantRead(workerFunction);
    lancamentosQueue.grantConsumeMessages(workerFunction);

    // SQS trigger (batch 10, report partial failures)
    workerFunction.addEventSource(new SqsEventSource(lancamentosQueue, {
      batchSize: 10,
      reportBatchItemFailures: true,
    }));

    // EventBridge schedule: every 5 minutes
    new events.Rule(this, 'ConsolidadoSchedule', {
      schedule: events.Schedule.rate(cdk.Duration.minutes(5)),
      description: 'Recalculate consolidado every 5 minutes',
      targets: [new targets.LambdaFunction(workerFunction)],
    });

    // ── Outputs ───────────────────────────────────────────────────────────────
    new cdk.CfnOutput(this, 'ApiUrl', {
      value: httpApi.apiEndpoint,
      description: 'API Gateway endpoint URL',
    });

    new cdk.CfnOutput(this, 'SqsQueueUrl', {
      value: lancamentosQueue.queueUrl,
      description: 'SQS queue URL for LancamentoCriadoEvent',
    });

    new cdk.CfnOutput(this, 'RedisEndpoint', {
      value: `${redisCluster.attrPrimaryEndPointAddress}:${redisCluster.attrPrimaryEndPointPort}`,
      description: 'ElastiCache Redis primary endpoint',
    });
  }
}
