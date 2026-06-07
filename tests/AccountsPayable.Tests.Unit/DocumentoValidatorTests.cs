using ContasAPagar.Web.Helpers;
using FluentAssertions;

namespace AccountsPayable.Tests.Unit;

public class DocumentoValidatorTests
{
    [Theory]
    [InlineData("11144477735", true)]   // CPF valido
    [InlineData("111.444.777-35", true)] // com mascara
    [InlineData("11111111111", false)]  // todos iguais
    [InlineData("12345678900", false)]  // digito errado
    [InlineData("123", false)]          // tamanho invalido
    public void ValidarCpf(string cpf, bool esperado) =>
        DocumentoValidator.ValidarCpf(cpf).Should().Be(esperado);

    [Theory]
    [InlineData("11222333000181", true)]    // CNPJ valido
    [InlineData("11.222.333/0001-81", true)] // com mascara
    [InlineData("11111111111111", false)]   // todos iguais
    [InlineData("11222333000100", false)]   // digito errado
    public void ValidarCnpj(string cnpj, bool esperado) =>
        DocumentoValidator.ValidarCnpj(cnpj).Should().Be(esperado);

    [Fact]
    public void SomenteDigitos_RemoveMascara() =>
        DocumentoValidator.SomenteDigitos("11.222.333/0001-81").Should().Be("11222333000181");
}
