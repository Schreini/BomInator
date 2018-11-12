using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BomInator
{
    interface IEncodingAnalyzer
    {
        Encoding Analyze(byte[] text);
    }

    public class EncodingAnalyzer : IEncodingAnalyzer
    {
        public Encoding Analyze(byte[] text)
        {
            List<Encoding> encodings = new List<Encoding>() {Encoding.UTF8, Encoding.Unicode};

            foreach (var encoding in encodings)
            {
                if (Analyze(text, encoding))
                    return encoding;
            }

            return new UnknownEncoding();
        }

        private bool Analyze(byte[] text, Encoding encoding)
        {
            var bom = encoding.GetPreamble();

            if (text.Length < bom.Length)
                return false;

            var textBom = new byte[bom.Length];
            Array.Copy(text, textBom, bom.Length);

            if (bom.SequenceEqual(textBom))
                return true;

            return false;
        }
    }
}
