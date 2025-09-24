using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatAppServer.Services
{
    public class NavService
    {
        Action<object> Navigate;
        public NavService(Action<object> _Navigate)
        {
            Navigate = _Navigate;
        }

        public void NavigateTo(object viewModel)
        {
            Navigate.Invoke(viewModel);
        }
    }
}
