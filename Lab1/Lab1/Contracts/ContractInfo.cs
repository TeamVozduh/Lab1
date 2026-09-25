using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lab1
{
    public sealed record ContractInfo(
        string Title,
        string Pre,
        string Post,
        string Effects,
        string Exceptions,
        string CorrectExample,
        string IncorrectExample);
}
