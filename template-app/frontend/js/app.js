// Aplicação de exemplo do template Infra-Aws: entrada de dados → transformação → relatório em tela.
// Personalize as telas, mas mantenha: login pelo Cognito (auth.js), chamadas só em /api (api.js)
// e textos sempre via h()/textContent (ui.js) — nunca innerHTML com dados.
import { tratarRetorno, sessao, entrar, sair, carregarConfig } from './auth.js';
import { api } from './api.js';
import { h, tabela, campo, aviso, usd } from './ui.js';

const mesAtual = () => new Date().toISOString().slice(0, 7);
let mes = mesAtual();
let cfg = null;

const ROTAS = { lancamentos: ['Lançamentos', telaLancamentos], importar: ['Importar CSV', telaImportar], relatorio: ['Relatório', telaRelatorio] };

function seletorMes(aoMudar) {
  const s = h('input', { type: 'month', value: mes, 'aria-label': 'Mês' });
  s.addEventListener('change', () => { mes = s.value || mesAtual(); aoMudar(); });
  return s;
}

async function telaLancamentos(recarregar) {
  const lista = await api('GET', '/registros?mes=' + mes);
  const msg = h('div', {});
  const data = h('input', { type: 'date', required: true, value: new Date().toISOString().slice(0, 10) });
  const categoria = h('input', { required: true, maxlength: 60, placeholder: 'ex.: Viagem' });
  const descricao = h('input', { maxlength: 200 });
  const valor = h('input', { type: 'number', required: true, min: '0.01', step: '0.01' });
  const form = h('form', {
    class: 'cartao', onsubmit: async (ev) => {
      ev.preventDefault();
      try {
        await api('POST', '/registros', { data: data.value, categoria: categoria.value, descricao: descricao.value, valor: Number(valor.value) });
        recarregar();
      } catch (e) { msg.replaceChildren(aviso(e.message, 'erro')); }
    }
  }, h('h2', {}, 'Novo lançamento'),
    h('div', { class: 'grade' }, campo('Data', data), campo('Categoria', categoria), campo('Descrição', descricao), campo('Valor (R$)', valor)),
    msg, h('div', { class: 'acoes' }, h('button', { class: 'botao primario', type: 'submit' }, 'Salvar')));

  const linhas = lista.map(r => [r.data, r.categoria, r.descricao, 'R$ ' + Number(r.valor).toLocaleString('pt-BR', { minimumFractionDigits: 2 }),
    h('button', { class: 'botao pequeno perigo', onclick: async () => { if (confirm('Remover este lançamento?')) { await api('DELETE', '/registros/' + r.id); recarregar(); } } }, 'Remover')]);
  return [h('header', { class: 'cabecalho' }, h('h1', {}, 'Lançamentos'), seletorMes(recarregar)), form,
    h('section', { class: 'cartao' }, tabela(['Data', 'Categoria', 'Descrição', 'Valor', ''], linhas, 'Nenhum lançamento neste mês.'))];
}

async function telaImportar() {
  const arquivo = h('input', { type: 'file', accept: '.csv,text/csv' });
  const texto = h('textarea', { placeholder: 'data;categoria;descricao;valor\n18/11/2026;Viagem;Táxi;45,90' });
  const msg = h('div', {});
  arquivo.addEventListener('change', async () => { if (arquivo.files[0]) texto.value = await arquivo.files[0].text(); });
  const form = h('form', {
    class: 'cartao', onsubmit: async (ev) => {
      ev.preventDefault();
      try {
        const r = await api('POST', '/importar', texto.value);
        msg.replaceChildren(aviso(`${r.importados} lançamento(s) importado(s).`, 'ok'), ...r.erros.map(e => aviso(e, 'erro')));
      } catch (e) { msg.replaceChildren(aviso(e.message, 'erro')); }
    }
  }, campo('Arquivo CSV', arquivo, 'Formato: data;categoria;descricao;valor — até 500 linhas. Datas AAAA-MM-DD ou DD/MM/AAAA.'),
    campo('Conteúdo', texto), msg, h('div', { class: 'acoes' }, h('button', { class: 'botao primario', type: 'submit' }, 'Importar')));
  return [h('header', { class: 'cabecalho' }, h('h1', {}, 'Importar CSV')), form];
}

async function telaRelatorio(recarregar) {
  const r = await api('GET', '/relatorio?mes=' + mes);
  const brl = (v) => 'R$ ' + Number(v).toLocaleString('pt-BR', { minimumFractionDigits: 2 });
  const barra = (pct) => { const b = h('div', { class: 'barra' }); b.style.width = Math.max(1, pct) + '%'; return h('div', { class: 'trilho' }, b); };
  return [
    h('header', { class: 'cabecalho' }, h('h1', {}, 'Relatório do mês'), seletorMes(recarregar)),
    h('section', { class: 'grade' },
      h('div', { class: 'cartao kpi' }, h('span', {}, 'Total'), h('strong', {}, brl(r.total))),
      h('div', { class: 'cartao kpi' }, h('span', {}, 'Lançamentos'), h('strong', {}, r.quantidade)),
      h('div', { class: 'cartao kpi' }, h('span', {}, 'Maior lançamento'), h('strong', {}, brl(r.maiorLancamento)))),
    h('section', { class: 'cartao' }, h('h2', {}, 'Por categoria'),
      tabela(['Categoria', 'Qtde', 'Total', '%', ''], r.porCategoria.map(l => [l.categoria, l.quantidade, brl(l.total), l.percentual + '%', barra(l.percentual)]), 'Sem dados no mês.')),
    h('section', { class: 'cartao' }, h('h2', {}, 'Por dia'),
      tabela(['Dia', 'Qtde', 'Total'], r.porDia.map(l => [l.categoria, l.quantidade, brl(l.total)]), 'Sem dados no mês.'))
  ];
}

async function renderizar() {
  const chave = location.hash.replace('#/', '') || 'lancamentos';
  const [titulo, tela] = ROTAS[chave] || ROTAS.lancamentos;
  const main = h('main', { tabindex: '-1' }, h('p', { class: 'dica' }, 'Carregando…'));
  document.getElementById('raiz').replaceChildren(h('div', { class: 'layout' },
    h('aside', { class: 'lateral' },
      h('div', { class: 'marca' }, h('img', { src: 'img/logo.svg', alt: '' }), h('span', { class: 'nome' }, cfg.aplicacao)),
      h('nav', { 'aria-label': 'Menu' }, Object.entries(ROTAS).map(([k, [t]]) => h('a', { href: '#/' + k, class: 'nav' + (t === titulo ? ' ativo' : '') }, t))),
      h('div', { class: 'usuario' }, h('small', {}, 'Plataforma Infra-Aws'), h('button', { class: 'botao pequeno', onclick: () => sair() }, 'Sair'))),
    main));
  try { main.replaceChildren(...await tela(renderizar)); }
  catch (e) { main.replaceChildren(aviso(e.message, 'erro')); }
}

(async () => {
  try {
    cfg = await carregarConfig();
    await tratarRetorno();
    if (!sessao()) return entrar();
    window.addEventListener('hashchange', renderizar);
    await renderizar();
  } catch (e) {
    document.getElementById('raiz').replaceChildren(h('div', { class: 'centro' }, aviso(e.message, 'erro')));
  }
})();
