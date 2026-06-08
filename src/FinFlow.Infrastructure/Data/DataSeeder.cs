using FinFlow.Domain.Entities;
using FinFlow.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace FinFlow.Data;

/// <summary>
/// Popula o banco com massa de teste realista (idempotente: so roda em banco vazio).
/// Gera fornecedores, cadastros base e 50 contas em estados variados:
/// pagas, vencidas, pendentes, parceladas, com imposto, parcialmente pagas,
/// aguardando aprovacao, com transacao bancaria fake e algumas conciliadas.
/// </summary>
public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Fornecedores.AnyAsync()) return; // ja populado

        var rnd = new Random(20240601); // semente fixa => massa reproduzivel
        var hoje = DateTime.Today;

        // Empresas (tenants). Os dados de teste pertencem à empresa 1.
        db.Empresas.AddRange(
            new Empresa { RazaoSocial = "Empresa Demo Matriz LTDA", Cnpj = "11222333000181" },
            new Empresa { RazaoSocial = "Empresa Demo Filial SA", Cnpj = "44555666000199" });
        await db.SaveChangesAsync();

        // ---- Categorias ----
        var categorias = new[]
        {
            "Aluguel", "Energia", "Internet", "Software",
            "Servicos", "Impostos", "Fornecedores", "Marketing"
        }.Select(n => new Categoria { Nome = n }).ToList();
        db.Categorias.AddRange(categorias);

        // ---- Centros de custo ----
        var centros = new[]
        {
            ("TI", "Tecnologia da Informacao"),
            ("COM", "Comercial"),
            ("RH", "Recursos Humanos"),
            ("FIN", "Financeiro"),
            ("OPS", "Operacoes")
        }.Select(t => new CentroCusto { Codigo = t.Item1, Nome = t.Item2 }).ToList();
        db.CentrosCusto.AddRange(centros);

        // ---- Contas bancarias da empresa ----
        var contasBanco = new List<ContaBancaria>
        {
            new() { Nome = "Conta Principal BB",   Banco = "Banco do Brasil", BancoIntegracao = BancoIntegracao.BancoDoBrasil, Agencia = "1234", Conta = "55667-8", SaldoInicial = 250000m },
            new() { Nome = "Conta Itau Pagamentos", Banco = "Itau",            BancoIntegracao = BancoIntegracao.Itau,         Agencia = "4567", Conta = "11223-4", SaldoInicial = 120000m },
            new() { Nome = "Conta Santander",       Banco = "Santander",       BancoIntegracao = BancoIntegracao.Santander,    Agencia = "8901", Conta = "99887-6", SaldoInicial = 80000m  },
        };
        db.ContasBancarias.AddRange(contasBanco);

        // ---- Fornecedores ----
        var nomes = new[]
        {
            "Tech Solutions LTDA", "Energia Brasil S.A.", "NetFibra Telecom",
            "CloudSoft Sistemas", "Servicos Gerais ME", "Imobiliaria Central",
            "Marketing Digital 360", "Papelaria Express", "Seguranca Total LTDA",
            "Consultoria Alfa"
        };
        var fornecedores = new List<Fornecedor>();
        for (int i = 0; i < nomes.Length; i++)
        {
            var bloqueado = i == 4; // um fornecedor bloqueado para testar regra
            fornecedores.Add(new Fornecedor
            {
                RazaoSocial = nomes[i],
                NomeFantasia = nomes[i].Split(' ')[0],
                TipoDocumento = TipoDocumento.Cnpj,
                Documento = GerarCnpj(rnd),
                Email = $"contato{i + 1}@fornecedor.com.br",
                Telefone = $"(11) 9{rnd.Next(1000, 9999)}-{rnd.Next(1000, 9999)}",
                Endereco = $"Rua das Empresas, {rnd.Next(10, 999)} - Sao Paulo/SP",
                Banco = contasBanco[i % 3].Banco,
                Agencia = $"{rnd.Next(1000, 9999)}",
                Conta = $"{rnd.Next(10000, 99999)}-{rnd.Next(0, 9)}",
                TipoConta = TipoContaBancaria.CorrentePessoaJuridica,
                ChavePix = $"contato{i + 1}@fornecedor.com.br",
                Status = bloqueado ? StatusFornecedor.Bloqueado : StatusFornecedor.Ativo
            });
        }
        db.Fornecedores.AddRange(fornecedores);

        await db.SaveChangesAsync(); // gera Ids dos cadastros base

        // ---- 50 contas a pagar em estados variados ----
        var contas = new List<ContaPagar>();
        var formaPgtos = Enum.GetValues<FormaPagamento>();

        for (int i = 0; i < 44; i++)
        {
            var forn = fornecedores[rnd.Next(fornecedores.Count)];
            var cat = categorias[rnd.Next(categorias.Count)];
            var cc = centros[rnd.Next(centros.Count)];
            var valor = Math.Round((decimal)(rnd.NextDouble() * 14000 + 200), 2);
            var vencimento = hoje.AddDays(rnd.Next(-40, 40));
            var forma = formaPgtos[rnd.Next(formaPgtos.Length)];

            var conta = new ContaPagar
            {
                Descricao = $"{cat.Nome} - {forn.NomeFantasia} #{i + 1:000}",
                FornecedorId = forn.Id,
                CategoriaId = cat.Id,
                CentroCustoId = cc.Id,
                ValorOriginal = valor,
                ValorLiquido = valor,
                DataEmissao = vencimento.AddDays(-30),
                DataCompetencia = vencimento.AddDays(-30),
                DataVencimento = vencimento,
                FormaPagamento = forma,
                CodigoBarras = forma == FormaPagamento.Boleto ? GerarLinhaDigitavel(rnd) : null,
                ChavePix = forma == FormaPagamento.Pix ? forn.ChavePix : null,
                Status = StatusConta.Pendente
            };

            // Algumas com imposto retido (ISS + IRRF) -> recalcula liquido
            if (i % 5 == 0)
            {
                var iss = Math.Round(valor * 0.05m, 2);
                var irrf = Math.Round(valor * 0.015m, 2);
                conta.Retencoes.Add(new RetencaoImposto { Tipo = TipoImposto.Iss, Aliquota = 5m, Valor = iss });
                conta.Retencoes.Add(new RetencaoImposto { Tipo = TipoImposto.Irrf, Aliquota = 1.5m, Valor = irrf });
                conta.ValorLiquido = valor - iss - irrf;
            }

            // Distribui estados de forma deterministica
            switch (i % 7)
            {
                case 0: // paga
                    conta.Status = StatusConta.Paga;
                    conta.ValorPago = conta.ValorLiquido;
                    conta.DataPagamento = vencimento.AddDays(-1);
                    conta.Baixas.Add(new BaixaPagamento
                    {
                        DataPagamento = conta.DataPagamento.Value,
                        ValorPago = conta.ValorLiquido,
                        ContaBancariaId = contasBanco[i % 3].Id,
                        FormaPagamento = forma,
                        Observacao = "Pagamento integral (seed)"
                    });
                    conta.Transacoes.Add(new BankTransaction
                    {
                        Banco = contasBanco[i % 3].BancoIntegracao,
                        TipoPagamento = forma,
                        Status = StatusTransacaoBancaria.Sucesso,
                        CodigoTransacao = $"E{rnd.Next(100000, 999999)}",
                        Valor = conta.ValorLiquido,
                        PayloadEnvio = "{ \"seed\": true }",
                        PayloadResposta = "{ \"status\": \"CONFIRMADO\" }",
                        DataRetorno = conta.DataPagamento,
                        StatusConciliacao = i % 14 == 0 ? StatusConciliacao.Conciliado : StatusConciliacao.NaoConciliado,
                        DataConciliacao = i % 14 == 0 ? conta.DataPagamento : null
                    });
                    break;

                case 1: // parcialmente paga
                    conta.Status = StatusConta.ParcialmentePaga;
                    conta.ValorPago = Math.Round(conta.ValorLiquido * 0.4m, 2);
                    conta.Baixas.Add(new BaixaPagamento
                    {
                        DataPagamento = hoje.AddDays(-3),
                        ValorPago = conta.ValorPago,
                        ContaBancariaId = contasBanco[i % 3].Id,
                        FormaPagamento = forma,
                        Observacao = "Pagamento parcial (seed)"
                    });
                    break;

                case 2: // vencida (vencimento no passado e nao paga)
                    conta.DataVencimento = hoje.AddDays(-rnd.Next(5, 30));
                    conta.Status = StatusConta.Vencida;
                    break;

                case 3: // aguardando aprovacao
                    conta.Status = StatusConta.EmAprovacao;
                    conta.Aprovacoes.Add(new Aprovacao
                    {
                        NivelExigido = conta.ValorLiquido > 10000m ? NivelAprovacao.Diretor
                                      : conta.ValorLiquido >= 1000m ? NivelAprovacao.Gerente
                                      : NivelAprovacao.Automatica,
                        Resultado = ResultadoAprovacao.Pendente
                    });
                    break;

                case 4: // aprovada e liberada
                    conta.Status = StatusConta.LiberadaParaPagamento;
                    conta.Aprovacoes.Add(new Aprovacao
                    {
                        NivelExigido = NivelAprovacao.Gerente,
                        Resultado = ResultadoAprovacao.Aprovada,
                        Aprovador = "gerente.financeiro",
                        DataDecisao = hoje.AddDays(-2),
                        Observacao = "Aprovado (seed)"
                    });
                    break;

                default: // pendente (5,6)
                    conta.Status = vencimento < hoje ? StatusConta.Vencida : StatusConta.Pendente;
                    break;
            }

            contas.Add(conta);
        }

        // ---- 1 compra parcelada em 6x (gera 6 contas vinculadas) ----
        var fornParcela = fornecedores[0];
        var catParcela = categorias.First(c => c.Nome == "Software");
        var ccParcela = centros.First(c => c.Codigo == "TI");
        var origem = new ContaPagar
        {
            Descricao = "Licenca ERP anual (compra parcelada 6x)",
            FornecedorId = fornParcela.Id,
            CategoriaId = catParcela.Id,
            CentroCustoId = ccParcela.Id,
            ValorOriginal = 6000m,
            ValorLiquido = 6000m,
            DataEmissao = hoje,
            DataCompetencia = hoje,
            DataVencimento = hoje.AddMonths(1),
            FormaPagamento = FormaPagamento.Boleto,
            Status = StatusConta.Pendente,
            TotalParcelas = 6,
            NumeroParcela = 0 // 0 = conta-mae (controle), parcelas reais sao 1..6
        };
        db.ContasPagar.Add(origem);
        await db.SaveChangesAsync();

        for (int p = 1; p <= 6; p++)
        {
            contas.Add(new ContaPagar
            {
                Descricao = $"Licenca ERP anual - parcela {p}/6",
                FornecedorId = fornParcela.Id,
                CategoriaId = catParcela.Id,
                CentroCustoId = ccParcela.Id,
                ValorOriginal = 1000m,
                ValorLiquido = 1000m,
                DataEmissao = hoje,
                DataCompetencia = hoje,
                DataVencimento = hoje.AddMonths(p),
                FormaPagamento = FormaPagamento.Boleto,
                CodigoBarras = GerarLinhaDigitavel(rnd),
                Status = StatusConta.Pendente,
                ContaOrigemId = origem.Id,
                NumeroParcela = p,
                TotalParcelas = 6
            });
        }

        db.ContasPagar.AddRange(contas);
        await db.SaveChangesAsync();

        await SeedReceberAsync(db, rnd, hoje);

        // Regras de aprovação padrão (espelham as alçadas por valor).
        db.RegrasAprovacao.AddRange(
            new RegraAprovacao { Nome = "Automatica ate R$ 1.000", ValorMinimo = 0m, ValorMaximo = 999.99m, NivelExigido = NivelAprovacao.Automatica },
            new RegraAprovacao { Nome = "Gerente R$ 1.000 a R$ 10.000", ValorMinimo = 1000m, ValorMaximo = 10000m, NivelExigido = NivelAprovacao.Gerente },
            new RegraAprovacao { Nome = "Diretor acima de R$ 10.000", ValorMinimo = 10000.01m, ValorMaximo = null, NivelExigido = NivelAprovacao.Diretor }
        );
        await db.SaveChangesAsync();

        await SeedChatAsync(db);
    }

    /// <summary>Conversas de exemplo do chat interno (Módulo 24).</summary>
    private static async Task SeedChatAsync(AppDbContext db)
    {
        var contaVinc = await db.ContasPagar.Where(c => c.NumeroParcela != 0).OrderBy(c => c.Id).FirstOrDefaultAsync();

        var geral = new ChatConversation
        {
            Titulo = "Financeiro ↔ Compras", Tipo = TipoConversa.Grupo, Area = AreaEmpresa.Financeiro,
            CriadoPor = "financeiro@demo.com",
            Participantes =
            {
                new ChatParticipant { Usuario = "financeiro@demo.com", Area = AreaEmpresa.Financeiro },
                new ChatParticipant { Usuario = "gerente@demo.com", Area = AreaEmpresa.Compras },
            },
            Mensagens =
            {
                new ChatMessage { Autor = "financeiro@demo.com", AutorArea = AreaEmpresa.Financeiro, Texto = "Pessoal, o pedido de notebooks já foi recebido?" },
                new ChatMessage { Autor = "gerente@demo.com", AutorArea = AreaEmpresa.Compras, Texto = "Recebido ontem, @financeiro@demo.com já pode lançar a conta." },
            }
        };
        db.Conversas.Add(geral);

        if (contaVinc is not null)
        {
            db.Conversas.Add(new ChatConversation
            {
                Titulo = $"Dúvida conta #{contaVinc.Id}", Tipo = TipoConversa.Grupo, Area = AreaEmpresa.Financeiro,
                ContaPagarId = contaVinc.Id, CriadoPor = "financeiro@demo.com",
                Participantes = { new ChatParticipant { Usuario = "financeiro@demo.com", Area = AreaEmpresa.Financeiro } },
                Mensagens = { new ChatMessage { Autor = "financeiro@demo.com", AutorArea = AreaEmpresa.Financeiro,
                    Texto = "Conversa vinculada ao pagamento — auditável.", VinculadaPagamento = true } }
            });
        }
        await db.SaveChangesAsync();
    }

    /// <summary>Massa de teste do modulo Contas a Receber (clientes + faturas variadas).</summary>
    private static async Task SeedReceberAsync(AppDbContext db, Random rnd, DateTime hoje)
    {
        var nomesClientes = new[]
        {
            "Mercado Central LTDA", "Construtora Horizonte", "Clinica Vida SA",
            "Auto Pecas Veloz", "Restaurante Sabor & Cia", "Distribuidora Norte"
        };
        var clientes = new List<Cliente>();
        for (int i = 0; i < nomesClientes.Length; i++)
        {
            clientes.Add(new Cliente
            {
                RazaoSocial = nomesClientes[i],
                NomeFantasia = nomesClientes[i].Split(' ')[0],
                TipoDocumento = TipoDocumento.Cnpj,
                Documento = GerarCnpj(rnd),
                Email = $"financeiro{i + 1}@cliente.com.br",
                Telefone = $"(11) 9{rnd.Next(1000, 9999)}-{rnd.Next(1000, 9999)}",
                Status = StatusFornecedor.Ativo
            });
        }
        db.Clientes.AddRange(clientes);
        await db.SaveChangesAsync();

        var contas = new List<ContaReceber>();
        for (int i = 0; i < 24; i++)
        {
            var cli = clientes[rnd.Next(clientes.Count)];
            var valor = Math.Round((decimal)(rnd.NextDouble() * 9000 + 300), 2);
            var venc = hoje.AddDays(rnd.Next(-30, 45));
            var conta = new ContaReceber
            {
                Descricao = $"Fatura {cli.NomeFantasia} #{i + 1:000}",
                ClienteId = cli.Id,
                Valor = valor,
                DataEmissao = venc.AddDays(-30),
                DataVencimento = venc,
                FormaRecebimento = FormaPagamento.Boleto,
                Status = StatusReceber.Aberta
            };

            switch (i % 4)
            {
                case 0: // recebida
                    conta.Status = StatusReceber.Recebida;
                    conta.ValorRecebido = valor;
                    conta.DataRecebimento = venc.AddDays(-2);
                    conta.Recebimentos.Add(new RecebimentoBaixa { DataRecebimento = venc.AddDays(-2), ValorRecebido = valor, FormaRecebimento = FormaPagamento.Pix });
                    break;
                case 1: // parcial
                    conta.Status = StatusReceber.ParcialmenteRecebida;
                    conta.ValorRecebido = Math.Round(valor * 0.5m, 2);
                    conta.Recebimentos.Add(new RecebimentoBaixa { DataRecebimento = hoje.AddDays(-2), ValorRecebido = conta.ValorRecebido, FormaRecebimento = FormaPagamento.Ted });
                    break;
                case 2: // vencida (inadimplente)
                    conta.DataVencimento = hoje.AddDays(-rnd.Next(5, 25));
                    conta.Status = StatusReceber.Vencida;
                    break;
                default:
                    conta.Status = venc < hoje ? StatusReceber.Vencida : StatusReceber.Aberta;
                    break;
            }
            contas.Add(conta);
        }
        db.ContasReceber.AddRange(contas);
        await db.SaveChangesAsync();
    }

    // CNPJ ficticio (apenas digitos, sem validacao oficial - massa de teste).
    private static string GerarCnpj(Random rnd)
    {
        var s = "";
        for (int i = 0; i < 14; i++) s += rnd.Next(0, 10);
        return s;
    }

    private static string GerarLinhaDigitavel(Random rnd)
    {
        var s = "";
        for (int i = 0; i < 47; i++) s += rnd.Next(0, 10);
        return s;
    }
}
