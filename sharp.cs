using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ulc
{
    public partial class sharp : Component
    {
        public sharp()
        {
            InitializeComponent();
        }

        public sharp(IContainer container)
        {
            container.Add(this);

            InitializeComponent();
        }
    }
}
