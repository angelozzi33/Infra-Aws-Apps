// Utilitários de interface. Regra de segurança: NUNCA usar innerHTML com dados —
// todo texto entra como textContent (o navegador não interpreta HTML/script vindo da API).

export function h(tag, atributos = {}, ...filhos) {
  const el = document.createElement(tag);
  for (const [chave, valor] of Object.entries(atributos || {})) {
    if (valor === undefined || valor === null || valor === false) continue;
    if (chave.startsWith('on') && typeof valor === 'function') el.addEventListener(chave.slice(2), valor);
    else if (chave === 'class') el.className = valor;
    else if (valor === true) el.setAttribute(chave, '');
    else el.setAttribute(chave, String(valor));
  }
  for (const filho of filhos.flat()) {
    if (filho === undefined || filho === null || filho === false) continue;
    el.append(filho instanceof Node ? filho : document.createTextNode(String(filho)));
  }
  return el;
}

export const dataHora = (iso) => iso ? new Date(iso).toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' }) : '—';
export const usd = (v) => 'US$ ' + Number(v || 0).toLocaleString('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 });

const STATUS = {
  EmValidacao: ['Em validação', 'az'], EmAprovacao: ['Em aprovação', 'am'], Provisionando: ['Provisionando', 'az'],
  Concluida: ['Concluída', 'vd'], AguardandoExecucaoManual: ['Aprovada (execução manual)', 'vd'],
  Devolvida: ['Devolvida', 'vm'], Reprovada: ['Reprovada', 'vm'], AjusteSolicitado: ['Ajuste solicitado', 'am'],
  Expirada: ['Expirada', 'cz'], FalhaProvisionamento: ['Falha', 'vm']
};
export function selo(status) {
  const [texto, cor] = STATUS[status] || [status, 'cz'];
  return h('span', { class: `selo ${cor}` }, texto);
}
export const seloSimples = (texto, cor = 'cz') => h('span', { class: `selo ${cor}` }, texto);

export const ROTULOS = {
  NovaAplicacao: 'Nova aplicação', Alteracao: 'Alteração', Transferencia: 'Transferência de dono',
  Descomissionamento: 'Descomissionamento', Excecao: 'Exceção',
  Publica: 'Pública', Interna: 'Interna', Confidencial: 'Confidencial', Pessoal: 'Pessoal (LGPD)',
  Departamental: 'Departamental', Critica: 'Crítica',
  ate20: 'Até 20', ate100: '21 a 100', mais100: 'Mais de 100',
  solicitante: 'Solicitante', gestor: 'Gestor aprovador', plataforma: 'Plataforma (SRE)',
  seguranca: 'Segurança', admin: 'Admin de cadastro', auditor: 'Auditor'
};
export const rotulo = (v) => ROTULOS[v] || v;

export function tabela(colunas, linhas, vazio = 'Nenhum registro.') {
  if (!linhas.length) return h('p', { class: 'dica' }, vazio);
  return h('div', { class: 'rolagem' },
    h('table', {},
      h('thead', {}, h('tr', {}, colunas.map(c => h('th', { scope: 'col' }, c)))),
      h('tbody', {}, linhas.map(l => h('tr', {}, l.map(c => h('td', {}, c)))))));
}

export function campo(rotuloTexto, controle, dica) {
  const id = controle.id || ('c' + Math.random().toString(36).slice(2, 9));
  controle.id = id;
  return h('div', { class: 'campo' }, h('label', { for: id }, rotuloTexto), controle, dica ? h('p', { class: 'dica' }, dica) : null);
}

export function opcoes(lista, selecionado) {
  return lista.map(v => h('option', { value: v, selected: v === selecionado }, rotulo(v)));
}

export function aviso(texto, tipo = 'info') { return h('div', { class: `aviso ${tipo}`, role: tipo === 'erro' ? 'alert' : 'status' }, texto); }
