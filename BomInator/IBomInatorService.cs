﻿using System;
using System.IO;
using System.Linq;
using System.Text;

namespace BomInator
{
    public interface IBomInatorService
    {
        //void BomInate(Stream input, Stream output);
        bool NeedsBom(byte[] input);
    }

    public class BomInatorService : IBomInatorService
    {
        public Encoding TargetEncoding => Encoding.UTF8;
        public byte[] Bom => TargetEncoding.GetPreamble();

        public bool NeedsBom(byte[] input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));

            var inputBom = new byte[Bom.Length];  // todo: use field for memory optimization?
            Array.Copy(input, inputBom, Bom.Length);

            if (Bom.SequenceEqual(inputBom))
            {
                return false;
            }

            return true;
        }
    }
}