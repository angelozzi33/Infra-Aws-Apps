import { sessao, entrar, reautenticar, carregarConfig } from './auth.js';

export class ErroApi extends Error {
  constructor(status, codigo, mensagem) { super(mensagem); this.status = status; this.codigo = codigo; }
}

/** Chamada à API do Portal, no MESMO domínio (/api) — sem CORS. */
export async function api(metodo, caminho, corpo) {
  const cfg = await carregarConfig();
  const s = sessao();
  if (!s) { await entrar(); throw new ErroApi(401, 'SEM_SESSAO', 'Redirecionando para o login…'); }
  const r = await fetch(cfg.apiBase + caminho, {
    method: metodo,
    headers: { 'Authorization': 'Bearer ' + s.accessToken, 'Content-Type': typeof corpo === 'string' ? 'text/plain' : 'application/json' },
    body: corpo === undefined ? undefined : (typeof corpo === 'string' ? corpo : JSON.stringify(corpo))
  });
  const dados = await r.json().catch(() => ({}));
  if (r.status === 401) { await entrar(); throw new ErroApi(401, 'SEM_SESSAO', 'Sessão expirada.'); }
  if (!r.ok) throw new ErroApi(r.status, dados.erro, dados.mensagem || 'Erro na requisição.');
  return dados;
}

/** Executa uma ação sensível; se o backend pedir MFA recente, oferece novo login. */
export async function acaoSensivel(fn) {
  try { return await fn(); }
  catch (e) {
    if (e instanceof ErroApi && e.codigo === 'MFA_EXPIRADO') {
      if (confirm(e.message + '\n\nEntrar novamente agora?')) await reautenticar();
    }
    throw e;
  }
}
