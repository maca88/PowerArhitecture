﻿using System;
using SharperArchitecture.Common.Specifications;

namespace SharperArchitecture.Common.Cryptographics
{
    public class Sha1Cryptography : ICryptography
    {
        public string Encrypt(string value)
        {
            return value.GetSha1Hash();
        }

        public bool IsDecryptSupported { get { return false; } }

        public string Decrypt(string value)
        {
            throw new NotSupportedException();
        }
    }
}