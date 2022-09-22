using WebFormsCore.Serializer;

namespace WebForms.Tests
{
    public class ViewStateSerializer
    {
        [Theory]
        [InlineData("1", 3)]
        [InlineData("", 1)]
        [InlineData(null, 1)]
        public void String(string? expected, int expectedLength)
        {
            var serializer = new StringViewStateSerializer();
            Span<byte> span = stackalloc byte[1024];

            serializer.TryWrite(expected, span, out var writeLength);
            Assert.Equal(expectedLength, writeLength);

            var result = serializer.Read(span, out var readLength);
            Assert.Equal(expectedLength, readLength);
            Assert.Equal(expected, result);
        }
    }
}