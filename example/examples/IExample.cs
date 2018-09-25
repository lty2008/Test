using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace example
{
    public interface IExample
    {
        string Name
        {
            get;
        }
        string ID
        {
            get;
        }

        Task Start();
    }
}
