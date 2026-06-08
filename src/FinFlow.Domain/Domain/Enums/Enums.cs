namespace FinFlow.Domain.Enums;

/// <summary>Situacao cadastral do fornecedor. Fornecedor Bloqueado/Inativo nao recebe pagamento.</summary>
public enum StatusFornecedor
{
    Ativo = 1,
    Bloqueado = 2,
    Inativo = 3
}

/// <summary>Pessoa fisica ou juridica - muda a validacao do documento (CPF x CNPJ).</summary>
public enum TipoDocumento
{
    Cnpj = 1,
    Cpf = 2
}

/// <summary>Tipo da conta bancaria do fornecedor / conta interna.</summary>
public enum TipoContaBancaria
{
    CorrentePessoaJuridica = 1,
    CorrentePessoaFisica = 2,
    Poupanca = 3,
    Pagamento = 4
}

/// <summary>
/// Ciclo de vida de uma conta a pagar. A ordem reflete o fluxo real:
/// Rascunho -> Pendente -> EmAprovacao -> Aprovada -> LiberadaParaPagamento -> Paga.
/// Estados terminais/alternativos: Reprovada, Vencida, Cancelada, Estornada, ParcialmentePaga.
/// </summary>
public enum StatusConta
{
    Rascunho = 1,
    Pendente = 2,
    EmAprovacao = 3,
    Aprovada = 4,
    Reprovada = 5,
    LiberadaParaPagamento = 6,
    Paga = 7,
    Vencida = 8,
    Cancelada = 9,
    Estornada = 10,
    ParcialmentePaga = 11
}

/// <summary>Forma pela qual a conta sera/foi paga.</summary>
public enum FormaPagamento
{
    Pix = 1,
    Ted = 2,
    Boleto = 3,
    Dinheiro = 4,
    CartaoCorporativo = 5,
    DebitoAutomatico = 6
}

/// <summary>Impostos retidos na fonte suportados pelo sistema.</summary>
public enum TipoImposto
{
    Iss = 1,
    Inss = 2,
    Irrf = 3,
    Pis = 4,
    Cofins = 5,
    Csll = 6
}

/// <summary>Nivel de alcada exigido conforme o valor da conta.</summary>
public enum NivelAprovacao
{
    /// <summary>Abaixo de R$ 1.000 - aprovacao automatica.</summary>
    Automatica = 1,
    /// <summary>Entre R$ 1.000 e R$ 10.000 - exige gerente.</summary>
    Gerente = 2,
    /// <summary>Acima de R$ 10.000 - exige diretor.</summary>
    Diretor = 3
}

/// <summary>Resultado de um passo de aprovacao.</summary>
public enum ResultadoAprovacao
{
    Pendente = 0,
    Aprovada = 1,
    Reprovada = 2
}

/// <summary>Banco usado na integracao de pagamento (fake nesta versao de estudo).</summary>
public enum BancoIntegracao
{
    Generico = 0,
    BancoDoBrasil = 1,
    Itau = 2,
    Santander = 3
}

/// <summary>Status retornado pela integracao bancaria simulada.</summary>
public enum StatusTransacaoBancaria
{
    Pendente = 1,   // enviado / em processamento
    Sucesso = 2,    // confirmado
    Erro = 3,       // recusado
    Estornado = 4   // pagamento revertido
}

/// <summary>Situacao de um lancamento na conciliacao bancaria.</summary>
public enum StatusConciliacao
{
    NaoConciliado = 1,
    Conciliado = 2,
    Divergente = 3
}

/// <summary>Áreas/departamentos da empresa (chat interno, Módulo 24).</summary>
public enum AreaEmpresa
{
    Financeiro = 1,
    Compras = 2,
    RH = 3,
    Fiscal = 4,
    Juridico = 5,
    Diretoria = 6,
    Operacoes = 7,
    TI = 8,
    Auditoria = 9
}

/// <summary>Tipo de conversa do chat interno.</summary>
public enum TipoConversa
{
    Individual = 1,
    Area = 2,
    Grupo = 3
}

/// <summary>Ciclo de vida de uma solicitação de compra (Fase 8).</summary>
public enum StatusCompra
{
    Solicitada = 1,
    Aprovada = 2,
    Reprovada = 3,
    PedidoEmitido = 4,
    Recebida = 5,
    Cancelada = 6
}

/// <summary>Severidade visual de uma notificação.</summary>
public enum SeveridadeNotificacao
{
    Info = 1,
    Sucesso = 2,
    Alerta = 3
}

/// <summary>Tipo de documento anexado a uma conta.</summary>
public enum TipoAnexo
{
    NotaFiscal = 1,
    Boleto = 2,
    Contrato = 3,
    Comprovante = 4,
    Recibo = 5,
    Outro = 6
}

/// <summary>Situacao de uma conta a receber (fatura de cliente).</summary>
public enum StatusReceber
{
    Aberta = 1,
    ParcialmenteRecebida = 2,
    Recebida = 3,
    Vencida = 4,
    Cancelada = 5
}

/// <summary>Acoes auditadas no sistema (trilha de auditoria).</summary>
public enum AcaoAuditoria
{
    Criacao = 1,
    EdicaoValor = 2,
    AlteracaoVencimento = 3,
    Aprovacao = 4,
    Reprovacao = 5,
    Pagamento = 6,
    Cancelamento = 7,
    Estorno = 8,
    Conciliacao = 9
}
