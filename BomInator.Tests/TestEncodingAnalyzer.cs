using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace BomInator.Tests
{
    public class TestEncodingAnalyzer
    {
        [Fact]
        public async Task AnalyzeShouldRecognizeUtf8Bom()
        {
            //Arrange
            var text = await File.ReadAllBytesAsync(@"testFiles/Utf8Bom.txt");
            var target = new EncodingAnalyzer();

            //Act
            var actual = target.Analyze(text);

            //Assert
            Assert.Equal(Encoding.UTF8, actual);
        }

        [Fact]
        public async Task AnalyzeShouldRecognizeUcs2LeBom()
        {
            //Arrange
            var text = await File.ReadAllBytesAsync(@"testFiles/Ucs2LeBom.txt");
            var target = new EncodingAnalyzer();

            //Act
            var actual = target.Analyze(text);

            //Assert
            Assert.Equal(Encoding.Unicode, actual);
        }

        [Fact]
        public void AnalyzeShouldReturnUnknownEncodingOnEmptyInput()
        {
            //Arrange
            var target = new EncodingAnalyzer();

            //Act
            var actual = target.Analyze(new byte[0]);

            //Assert
            Assert.Equal(new UnknownEncoding(), actual);
        }
    }
}
