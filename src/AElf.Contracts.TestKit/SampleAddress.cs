using AElf.Types;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace AElf.Contracts.TestKit
{
    public static class SampleAddress
    {
        public static readonly IReadOnlyList<Address> AddressList;

        private static readonly string[] Strings =
        {
            "2EM5uV6bSJh6xJfZTUa1pZpYsYcCUAdPvZvFUJzMDJEx3rbioz",
            "2ktxGpyiYCjFU5KwuXtbBckczX6uPmEtesJEsQPqMukcHZFY9a",
            "2LdrKw6vi2uWSSGhiS1MBUPANFuhJzBPYDsQ65Jm7C2uEy5KKW",
            "2ohojn441KmsVkaDS3wEL928gbpan352ZJ5ruMFxoa8iorUce",
            "2vNDCj1WjNLAXm3VnEeGGRMw3Aab4amVSEaYmCyxQKjNhLhfL7",
            "4v9cdSsn2PmZuFCxoSZhtY7Q2yUjdTNz6sQQdHNibdgaRg8Wx",
            "9Njc5pXW9Rw499wqSJzrfQuJQFVCcWnLNjZispJM4LjKmRPyq",
            "Lib8JSzdsFC7uCwvEwviadh3kp9LzaLMCauK4fSzrwc2qtHVi",
            "LYKSAU799wDphRK7W5ZsMBF2vDG8ijeuESk1R7Xpi6hBpdnX4",
            "XgCfhmyzhtYMfcEy9CbY6sjDAtThVwfRFRu66VhXQpjxNtQ6Q"
        };

        static SampleAddress()
        {
            AddressList = new ReadOnlyCollection<Address>(
                Strings.Select(AddressHelper.Base58StringToAddress).ToList());
        }
    }
}