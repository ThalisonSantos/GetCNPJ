using System.Globalization;
using System.Threading;
using GetCNPJ.Exceptions;
using GetCNPJ.Validation;
using Xunit;

namespace GetCNPJ.Tests
{
    public class CnpjValidatorTests
    {
        // Exemplo oficial RFB/Serpro, manual de cálculo, pp. 3–4: 459 % 11 = 8;
        // primeiro DV = 3; 424 % 11 = 6; segundo DV = 5. Demais vetores são sintéticos.
        [Theory]
        [InlineData("12ABC34501DE35", "12ABC34501DE35")]
        [InlineData("12.ABC.345/01DE-35", "12ABC34501DE35")]
        [InlineData("12.abc.345/01de-35", "12ABC34501DE35")]
        [InlineData(" \t12.abc.345/01de-35\r\n", "12ABC34501DE35")]
        [InlineData("12345678000195", "12345678000195")]
        [InlineData("12.345.678/0001-95", "12345678000195")]
        [InlineData("00000000000191", "00000000000191")]
        [InlineData("00.000.000/0001-91", "00000000000191")]
        [InlineData("ABCDEFGHIJKL80", "ABCDEFGHIJKL80")]
        [InlineData("MNOPQRSTUVWX50", "MNOPQRSTUVWX50")]
        [InlineData("YZ012345678981", "YZ012345678981")]
        [InlineData("ZZZZZZZZZZZZ62", "ZZZZZZZZZZZZ62")]
        [InlineData("A0000000000709", "A0000000000709")] // Primeiro DV: resto 0.
        [InlineData("A0000000000202", "A0000000000202")] // Primeiro DV: resto 1.
        public void ValidateAndNormalize_WithValidInput_PreservesIdentifier(string input, string expected)
        {
            Assert.Equal(expected, CnpjValidator.ValidateAndNormalize(input));
            Assert.True(CnpjValidator.IsValid(input));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" \t\r\n")]
        [InlineData("1234567800019")]
        [InlineData("123456780001950")]
        [InlineData("12345678000194")]
        [InlineData("12ABC34501DE34")]
        [InlineData("12ABC34501DE25")]
        [InlineData("12ABC34501DEA5")]
        [InlineData("12ABC34501DE3B")]
        [InlineData("00000000000000")]
        [InlineData("11111111111111")]
        [InlineData("22222222222222")]
        [InlineData("33333333333333")]
        [InlineData("44444444444444")]
        [InlineData("55555555555555")]
        [InlineData("66666666666666")]
        [InlineData("77777777777777")]
        [InlineData("88888888888888")]
        [InlineData("99999999999999")]
        [InlineData("A12345678000195")]
        [InlineData("12345678000195!")]
        [InlineData("12@BC34501DE35")]
        [InlineData("12ÁBC34501DE35")]
        [InlineData("１２ABC34501DE35")]
        [InlineData("١٢ABC34501DE35")]
        [InlineData("12АBC34501DE35")] // А cirílico, não A ASCII.
        [InlineData("12ABC34501DK35")]
        [InlineData("12ABC34501Dſ35")]
        [InlineData("12ABC34501Dı35")]
        [InlineData("12ABC34501DE٣5")]
        [InlineData("12ABC34501DE3５")]
        [InlineData("12 ABC34501DE35")]
        [InlineData("12ABC\t34501DE35")]
        [InlineData("12ABC34501DE35\0")]
        [InlineData("12-ABC-345/01DE.35")]
        [InlineData("12.ABC.345/01DE/35")]
        [InlineData("12..ABC.345/01DE-35")]
        [InlineData("12.ABC.345/01DE-35/injecao")]
        public void ValidateAndNormalize_WithInvalidInput_RejectsWithoutRemovingCharacters(string? input)
        {
            Assert.False(CnpjValidator.IsValid(input));
            Assert.Throws<InvalidCnpjException>(() => CnpjValidator.ValidateAndNormalize(input));
        }

        [Theory]
        [InlineData("12345678000195", "12.345.678/0001-95")]
        [InlineData("00000000000191", "00.000.000/0001-91")]
        [InlineData("12ABC34501DE35", "12.ABC.345/01DE-35")]
        [InlineData("12abc34501de35", "12.abc.345/01de-35")]
        [InlineData("12.ABC.345/01DE-35", "12.ABC.345/01DE-35")]
        [InlineData("incompleto", "incompleto")]
        [InlineData("", "")]
        [InlineData(null, null)]
        public void Format_PreservesCharactersAndLegacyPresentationBehavior(string? input, string? expected)
        {
            Assert.Equal(expected, CnpjValidator.Format(input!));
        }

        [Fact]
        public void ValidateAndNormalize_IsIndependentOfCurrentCulture()
        {
            var originalCulture = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
                Assert.Equal("ABCDEFGHIJKL80", CnpjValidator.ValidateAndNormalize("abcdefghijkl80"));
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = originalCulture;
            }
        }
    }
}
