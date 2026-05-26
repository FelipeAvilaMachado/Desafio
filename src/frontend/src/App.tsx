import { useEffect, useMemo, useState } from 'react';
import './App.css';

interface LancamentoDto {
  id: string;
  tipo: 'Debito' | 'Credito';
  valor: number;
  descricao: string;
  data: string;
  criadoEm: string;
}

interface ConsolidadoDiarioDto {
  data: string;
  totalDebitos: number;
  totalCreditos: number;
  saldo: number;
  atualizadoEm: string;
}

interface DispararConsolidacaoResult {
  datasProcessadas: number;
  datas: string[];
  totalDebitos: number;
  totalCreditos: number;
  saldo: number;
}

interface LoadStats {
  total: number;
  success: number;
  failed: number;
  elapsedMs: number;
  reqPerSecond: number;
  p50Ms?: number;
  p95Ms?: number;
  p99Ms?: number;
  errorSamples?: string[];
  source?: 'browser' | 'server';
}

const pad2 = (value: number) => value.toString().padStart(2, '0');
const formatCurrency = (value: number) =>
  new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value);

const todayIso = (() => {
  const now = new Date();
  return `${now.getFullYear()}-${pad2(now.getMonth() + 1)}-${pad2(now.getDate())}`;
})();

const monthStartIso = (() => {
  const now = new Date();
  return `${now.getFullYear()}-${pad2(now.getMonth() + 1)}-01`;
})();

const randomAmount = () => Number((Math.random() * 950 + 50).toFixed(2));
const randomType = () => (Math.random() < 0.5 ? 'Debito' : 'Credito') as 'Debito' | 'Credito';
const maxLoadTotal = 5000;
const maxLoadConcurrency = 256;

const devApimKey = import.meta.env.VITE_APIM_SUBSCRIPTION_KEY || 'dev-apim-key-456';
const devEntraToken = import.meta.env.VITE_ENTRA_TOKEN || 'dev-entra-token-123';

async function apiFetch<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(path, {
    headers: {
      'Content-Type': 'application/json',
      'Ocp-Apim-Subscription-Key': devApimKey,
      Authorization: `Bearer ${devEntraToken}`,
      ...(init?.headers ?? {}),
    },
    ...init,
  });

  if (!response.ok) {
    const text = await response.text();
    throw new Error(text || `Erro HTTP ${response.status}`);
  }

  if (response.status === 204) {
    return null as T;
  }

  return (await response.json()) as T;
}

function App() {
  const [globalError, setGlobalError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [working, setWorking] = useState(false);

  const [lancamentos, setLancamentos] = useState<LancamentoDto[]>([]);
  const [lancamentoDetalhe, setLancamentoDetalhe] = useState<LancamentoDto | null>(null);
  const [consolidadoDiario, setConsolidadoDiario] = useState<ConsolidadoDiarioDto | null>(null);
  const [consolidadoPeriodo, setConsolidadoPeriodo] = useState<ConsolidadoDiarioDto[]>([]);
  const [consolidacaoManual, setConsolidacaoManual] = useState<DispararConsolidacaoResult | null>(null);
  const [loadStats, setLoadStats] = useState<LoadStats | null>(null);

  const [consultaData, setConsultaData] = useState(todayIso);
  const [periodoInicio, setPeriodoInicio] = useState(monthStartIso);
  const [periodoFim, setPeriodoFim] = useState(todayIso);

  const [novoTipo, setNovoTipo] = useState<'Debito' | 'Credito'>('Credito');
  const [novoValor, setNovoValor] = useState('100.00');
  const [novaDescricao, setNovaDescricao] = useState('Lançamento manual');
  const [novaData, setNovaData] = useState(todayIso);

  const [loadTotal, setLoadTotal] = useState(1000);
  const [loadConcurrency, setLoadConcurrency] = useState(48);
  const [dispararAposCarga, setDispararAposCarga] = useState(true);

  const resumo = useMemo(() => {
    let creditos = 0;
    let debitos = 0;

    for (const lancamento of lancamentos) {
      if (lancamento.tipo === 'Credito') creditos += lancamento.valor;
      else debitos += lancamento.valor;
    }

    return {
      total: lancamentos.length,
      creditos,
      debitos,
      saldo: creditos - debitos,
    };
  }, [lancamentos]);

  const refreshLancamentos = async (date: string) => {
    const url = date ? `/api/lancamentos?data=${encodeURIComponent(date)}` : '/api/lancamentos';
    const data = await apiFetch<LancamentoDto[]>(url);
    setLancamentos(data);
  };

  const refreshConsolidadoDiario = async (date: string) => {
    const response = await fetch(`/api/consolidado/diario?data=${encodeURIComponent(date)}`, {
      headers: {
        'Ocp-Apim-Subscription-Key': devApimKey,
        Authorization: `Bearer ${devEntraToken}`,
      },
    });
    if (response.status === 404) {
      setConsolidadoDiario(null);
      return;
    }

    if (!response.ok) {
      const text = await response.text();
      throw new Error(text || `Erro HTTP ${response.status}`);
    }

    const data = (await response.json()) as ConsolidadoDiarioDto;
    setConsolidadoDiario(data);
  };

  const refreshConsolidadoPeriodo = async (inicio: string, fim: string) => {
    const url = `/api/consolidado/periodo?dataInicio=${encodeURIComponent(inicio)}&dataFim=${encodeURIComponent(fim)}`;
    const data = await apiFetch<ConsolidadoDiarioDto[]>(url);
    setConsolidadoPeriodo(data);
  };

  const refreshAll = async () => {
    setGlobalError(null);
    setLoading(true);

    try {
      await Promise.all([
        refreshLancamentos(consultaData),
        refreshConsolidadoDiario(consultaData),
        refreshConsolidadoPeriodo(periodoInicio, periodoFim),
      ]);
    } catch (error) {
      setGlobalError(error instanceof Error ? error.message : 'Falha ao atualizar dados');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void refreshAll();
  }, []);

  const onCriarLancamento = async () => {
    setGlobalError(null);
    setWorking(true);

    try {
      const created = await apiFetch<LancamentoDto>('/api/lancamentos', {
        method: 'POST',
        body: JSON.stringify({
          tipo: novoTipo,
          valor: Number(novoValor),
          descricao: novaDescricao,
          data: novaData,
        }),
      });

      setLancamentoDetalhe(created);
      await refreshAll();
    } catch (error) {
      setGlobalError(error instanceof Error ? error.message : 'Falha ao criar lançamento');
    } finally {
      setWorking(false);
    }
  };

  const onBuscarLancamentoPorId = async (id: string) => {
    setGlobalError(null);

    try {
      const data = await apiFetch<LancamentoDto>(`/api/lancamentos/${id}`);
      setLancamentoDetalhe(data);
    } catch (error) {
      setGlobalError(error instanceof Error ? error.message : 'Falha ao buscar lançamento');
      setLancamentoDetalhe(null);
    }
  };

  const onDispararConsolidacao = async () => {
    setGlobalError(null);
    setWorking(true);

    try {
      const result = await apiFetch<DispararConsolidacaoResult>('/api/consolidado/disparar', {
        method: 'POST',
        body: JSON.stringify({ dataInicio: periodoInicio, dataFim: periodoFim }),
      });

      setConsolidacaoManual(result);
      await Promise.all([
        refreshConsolidadoDiario(consultaData),
        refreshConsolidadoPeriodo(periodoInicio, periodoFim),
      ]);
    } catch (error) {
      setGlobalError(error instanceof Error ? error.message : 'Falha ao disparar consolidação');
    } finally {
      setWorking(false);
    }
  };

  const onRodarCarga = async () => {
    setGlobalError(null);
    setWorking(true);

    const total = Math.max(1, loadTotal);
    const concurrency = Math.min(total, Math.max(1, loadConcurrency));
    let sent = 0;
    let success = 0;
    let failed = 0;

    const start = performance.now();

    const worker = async () => {
      while (sent < total) {
        const current = sent;
        sent += 1;

        const date = new Date(consultaData);
        date.setDate(date.getDate() - (current % 5));
        const payloadDate = `${date.getFullYear()}-${pad2(date.getMonth() + 1)}-${pad2(date.getDate())}`;

        try {
          await apiFetch<LancamentoDto>('/api/lancamentos', {
            method: 'POST',
            body: JSON.stringify({
              tipo: randomType(),
              valor: randomAmount(),
              descricao: `Carga #${current + 1}`,
              data: payloadDate,
            }),
          });
          success += 1;
        } catch {
          failed += 1;
        }
      }
    };

    try {
      await Promise.all(Array.from({ length: concurrency }, () => worker()));

      const elapsedMs = performance.now() - start;
      const reqPerSecond = elapsedMs > 0 ? (success / elapsedMs) * 1000 : success;

      setLoadStats({ total, success, failed, elapsedMs, reqPerSecond, source: 'browser' });

      if (dispararAposCarga) {
        await onDispararConsolidacao();
      } else {
        await refreshAll();
      }
    } catch (error) {
      setGlobalError(error instanceof Error ? error.message : 'Falha durante teste de carga');
    } finally {
      setWorking(false);
    }
  };

  const onRodarBenchmarkServidor = async () => {
    setGlobalError(null);
    setWorking(true);

    try {
      const result = await apiFetch<LoadStats>('/api/benchmark/carga', {
        method: 'POST',
        body: JSON.stringify({
          total: loadTotal,
          concorrencia: loadConcurrency,
          dataBase: consultaData,
        }),
      });

      setLoadStats({ ...result, source: 'server' });
      await refreshAll();
    } catch (error) {
      setGlobalError(error instanceof Error ? error.message : 'Falha no benchmark servidor');
    } finally {
      setWorking(false);
    }
  };

  const applyBurstPreset = () => {
    setGlobalError(null);
    setLoadStats(null);
    setLoadTotal(1000);
    setLoadConcurrency(64);
    setDispararAposCarga(false);
  };

  return (
    <div className="app-container">
      <header className="app-header">
        <p className="kicker">Desafio Financeiro</p>
        <h1 className="app-title">Painel de API + Carga + Consolidação</h1>
        <p className="app-subtitle">
          Simule tráfego de lançamentos, consulte o consolidado e dispare a consolidação sob demanda.
        </p>
      </header>

      <main className="main-content">
        {globalError && (
          <section className="error-banner" role="alert" aria-live="polite">
            {globalError}
          </section>
        )}

        <section className="card overview-card">
          <h2>Visão Geral</h2>
          <div className="metrics-grid">
            <article className="metric-card">
              <h3>Lançamentos</h3>
              <p>{resumo.total}</p>
            </article>
            <article className="metric-card">
              <h3>Créditos</h3>
              <p>{formatCurrency(resumo.creditos)}</p>
            </article>
            <article className="metric-card">
              <h3>Débitos</h3>
              <p>{formatCurrency(resumo.debitos)}</p>
            </article>
            <article className="metric-card">
              <h3>Saldo</h3>
              <p>{formatCurrency(resumo.saldo)}</p>
            </article>
          </div>
          <button type="button" className="secondary" onClick={refreshAll} disabled={loading || working}>
            {loading ? 'Atualizando...' : 'Atualizar dados'}
          </button>
        </section>

        <section className="card split-card">
          <div>
            <h2>Novo Lançamento</h2>
            <div className="form-grid">
              <label>
                Tipo
                <select value={novoTipo} onChange={(e) => setNovoTipo(e.target.value as 'Debito' | 'Credito')}>
                  <option value="Credito">Crédito</option>
                  <option value="Debito">Débito</option>
                </select>
              </label>
              <label>
                Valor
                <input value={novoValor} type="number" min="0.01" step="0.01" onChange={(e) => setNovoValor(e.target.value)} />
              </label>
              <label>
                Data
                <input value={novaData} type="date" onChange={(e) => setNovaData(e.target.value)} />
              </label>
              <label className="wide">
                Descrição
                <input value={novaDescricao} onChange={(e) => setNovaDescricao(e.target.value)} />
              </label>
            </div>
            <button type="button" className="primary" onClick={onCriarLancamento} disabled={working || loading}>
              Criar lançamento
            </button>
          </div>

          <div>
            <h2>Teste de Carga</h2>
            <div className="button-row" style={{ marginBottom: '0.85rem' }}>
              <button type="button" className="secondary" onClick={applyBurstPreset} disabled={working || loading}>
                Ativar burst
              </button>
            </div>
            <div className="form-grid compact">
              <label>
                Requests
                <input
                  type="number"
                  min={1}
                  max={maxLoadTotal}
                  value={loadTotal}
                  onChange={(e) => setLoadTotal(Math.min(maxLoadTotal, Number(e.target.value || 1)))}
                />
                <span className="hint">Máximo sugerido: {maxLoadTotal}</span>
              </label>
              <label>
                Concorrência
                <input
                  type="number"
                  min={1}
                  max={maxLoadConcurrency}
                  value={loadConcurrency}
                  onChange={(e) => setLoadConcurrency(Math.min(maxLoadConcurrency, Number(e.target.value || 1)))}
                />
                <span className="hint">Máximo sugerido: {maxLoadConcurrency}</span>
              </label>
              <label className="toggle-field wide">
                <input
                  type="checkbox"
                  checked={dispararAposCarga}
                  onChange={(e) => setDispararAposCarga(e.target.checked)}
                />
                Disparar consolidação automaticamente ao final
              </label>
            </div>
            <div className="button-row">
              <button type="button" className="primary" onClick={onRodarCarga} disabled={working || loading}>
                Rodar carga (browser)
              </button>
              <button type="button" className="secondary" onClick={onRodarBenchmarkServidor} disabled={working || loading}>
                Benchmark servidor
              </button>
            </div>

            {loadStats && (
              <div className="mini-panel">
                <strong>Resultado da carga</strong>
                <p>
                  {loadStats.success}/{loadStats.total} sucesso, {loadStats.failed} falha, {loadStats.reqPerSecond.toFixed(2)} req/s
                </p>
                <p>Origem: {loadStats.source === 'server' ? 'Servidor' : 'Browser'}</p>
                {typeof loadStats.p50Ms === 'number' && (
                  <p>
                    Latência p50/p95/p99: {loadStats.p50Ms.toFixed(2)}ms / {loadStats.p95Ms?.toFixed(2)}ms / {loadStats.p99Ms?.toFixed(2)}ms
                  </p>
                )}
                {loadStats.errorSamples && loadStats.errorSamples.length > 0 && (
                  <p>Erros exemplo: {loadStats.errorSamples.join(' | ')}</p>
                )}
              </div>
            )}
          </div>
        </section>

        <section className="card split-card">
          <div>
            <h2>Consulta de Dados</h2>
            <div className="form-grid compact">
              <label>
                Data diária
                <input type="date" value={consultaData} onChange={(e) => setConsultaData(e.target.value)} />
              </label>
              <label>
                Início período
                <input type="date" value={periodoInicio} onChange={(e) => setPeriodoInicio(e.target.value)} />
              </label>
              <label>
                Fim período
                <input type="date" value={periodoFim} onChange={(e) => setPeriodoFim(e.target.value)} />
              </label>
            </div>
            <div className="button-row">
              <button type="button" className="secondary" onClick={refreshAll} disabled={loading || working}>
                Recarregar consultas
              </button>
              <button type="button" className="primary" onClick={onDispararConsolidacao} disabled={loading || working}>
                Disparar consolidação
              </button>
            </div>

            {consolidacaoManual && (
              <div className="mini-panel">
                <strong>Consolidação manual</strong>
                <p>
                  {consolidacaoManual.datasProcessadas} data(s), saldo total {formatCurrency(consolidacaoManual.saldo)}
                </p>
              </div>
            )}
          </div>

          <div>
            <h2>Consolidado Diário ({consultaData})</h2>
            {consolidadoDiario ? (
              <div className="mini-panel">
                <p>Créditos: {formatCurrency(consolidadoDiario.totalCreditos)}</p>
                <p>Débitos: {formatCurrency(consolidadoDiario.totalDebitos)}</p>
                <p>Saldo: {formatCurrency(consolidadoDiario.saldo)}</p>
                <p>Atualizado em: {new Date(consolidadoDiario.atualizadoEm).toLocaleString()}</p>
              </div>
            ) : (
              <div className="mini-panel">
                <p>Nenhum consolidado encontrado para a data selecionada.</p>
              </div>
            )}
          </div>
        </section>

        <section className="card split-card">
          <div>
            <h2>Lançamentos ({consultaData})</h2>
            <div className="scroll-area">
              <table>
                <thead>
                  <tr>
                    <th>Tipo</th>
                    <th>Descrição</th>
                    <th>Valor</th>
                    <th>Ação</th>
                  </tr>
                </thead>
                <tbody>
                  {lancamentos.map((item) => (
                    <tr key={item.id}>
                      <td>{item.tipo}</td>
                      <td>{item.descricao}</td>
                      <td>{formatCurrency(item.valor)}</td>
                      <td>
                        <button type="button" className="inline-btn" onClick={() => onBuscarLancamentoPorId(item.id)}>
                          Ver
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>

          <div>
            <h2>Consolidado do Período</h2>
            <div className="scroll-area">
              <table>
                <thead>
                  <tr>
                    <th>Data</th>
                    <th>Créditos</th>
                    <th>Débitos</th>
                    <th>Saldo</th>
                  </tr>
                </thead>
                <tbody>
                  {consolidadoPeriodo.map((item) => (
                    <tr key={item.data}>
                      <td>{item.data}</td>
                      <td>{formatCurrency(item.totalCreditos)}</td>
                      <td>{formatCurrency(item.totalDebitos)}</td>
                      <td>{formatCurrency(item.saldo)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </section>

        <section className="card">
          <h2>Detalhe por ID</h2>
          {lancamentoDetalhe ? (
            <div className="mini-panel">
              <p>ID: {lancamentoDetalhe.id}</p>
              <p>Tipo: {lancamentoDetalhe.tipo}</p>
              <p>Descrição: {lancamentoDetalhe.descricao}</p>
              <p>Valor: {formatCurrency(lancamentoDetalhe.valor)}</p>
              <p>Data: {lancamentoDetalhe.data}</p>
              <p>Criado em: {new Date(lancamentoDetalhe.criadoEm).toLocaleString()}</p>
            </div>
          ) : (
            <div className="mini-panel">
              <p>Selecione um lançamento na lista para buscar por ID.</p>
            </div>
          )}
        </section>
      </main>

      <footer className="app-footer">
        <p>Frontend conectado aos endpoints reais de API para operação e carga.</p>
      </footer>
    </div>
  );
}

export default App;
