// Login com o Cognito usando Authorization Code + PKCE.
// PKCE: o navegador gera um segredo aleatório por login (code_verifier) e envia só o hash dele;
// assim, mesmo que alguém intercepte o "code" da URL, não consegue trocá-lo por tokens.
// Os tokens ficam no sessionStorage (somem ao fechar a aba); não guardamos refresh token.

let config = null;
const CHAVE = 'app.sessao';

export async function carregarConfig() {
  if (config) return config;
  const r = await fetch('config.json', { cache: 'no-store' });
  if (!r.ok) throw new Error('config.json não encontrado — o site foi implantado pelo CDK?');
  config = await r.json();
  return config;
}

const b64url = (bytes) => btoa(String.fromCharCode(...new Uint8Array(bytes))).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
const aleatorio = (n) => b64url(crypto.getRandomValues(new Uint8Array(n)));
const redirectUri = () => config.urlSite.replace(/\/$/, '') + '/';

export async function entrar() {
  await carregarConfig();
  const verificador = aleatorio(64);
  const estado = aleatorio(24);
  const desafio = b64url(await crypto.subtle.digest('SHA-256', new TextEncoder().encode(verificador)));
  sessionStorage.setItem('app.pkce', JSON.stringify({ verificador, estado }));
  const url = new URL(config.urlLogin + '/oauth2/authorize');
  url.search = new URLSearchParams({
    response_type: 'code', client_id: config.clientId, redirect_uri: redirectUri(),
    scope: 'openid email profile', code_challenge_method: 'S256', code_challenge: desafio, state: estado,
    lang: 'pt-BR'   // tela de login (managed login) em português (ADR-033 da plataforma)
  });
  location.assign(url.toString());
}

/** Se a URL tem ?code=..., troca pelo token. Retorna true se tratou um retorno de login. */
export async function tratarRetorno() {
  const params = new URLSearchParams(location.search);
  if (!params.has('code')) return false;
  await carregarConfig();
  const pkce = JSON.parse(sessionStorage.getItem('app.pkce') || '{}');
  sessionStorage.removeItem('app.pkce');
  history.replaceState(null, '', location.pathname + location.hash);
  if (!pkce.estado || params.get('state') !== pkce.estado) throw new Error('Retorno de login inválido (state). Tente novamente.');

  const r = await fetch(config.urlLogin + '/oauth2/token', {
    method: 'POST',
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
    body: new URLSearchParams({
      grant_type: 'authorization_code', client_id: config.clientId, code: params.get('code'),
      redirect_uri: redirectUri(), code_verifier: pkce.verificador
    })
  });
  if (!r.ok) throw new Error('Falha ao concluir o login.');
  const t = await r.json();
  sessionStorage.setItem(CHAVE, JSON.stringify({
    accessToken: t.access_token, idToken: t.id_token, expiraEm: Date.now() + (t.expires_in - 60) * 1000
  }));
  return true;
}

export function sessao() {
  const s = JSON.parse(sessionStorage.getItem(CHAVE) || 'null');
  return s && s.expiraEm > Date.now() ? s : null;
}

export function dadosDoToken() {
  const s = sessao();
  if (!s) return null;
  const corpo = s.idToken.split('.')[1].replace(/-/g, '+').replace(/_/g, '/');
  return JSON.parse(decodeURIComponent(escape(atob(corpo))));
}

export async function sair(motivo) {
  await carregarConfig();
  sessionStorage.removeItem(CHAVE);
  if (motivo) sessionStorage.setItem('app.motivo', motivo);
  const url = new URL(config.urlLogin + '/logout');
  url.search = new URLSearchParams({ client_id: config.clientId, logout_uri: redirectUri() });
  location.assign(url.toString());
}

/** Ação sensível com MFA antigo: encerra a sessão para exigir novo login (senha + MFA). */
export const reautenticar = () => sair('Para confirmar esta ação sensível, entre novamente com senha e MFA.');
