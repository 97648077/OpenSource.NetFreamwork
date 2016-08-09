using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenSource.Helps
{
    public class ValidtationResult
    {
        /// <summary>
        /// yes/no
        /// </summary>
        public Boolean Succeed { get; set; }

        /// <summary>
        /// error message
        /// </summary>
        public string Error { get; set; }
    }
}
