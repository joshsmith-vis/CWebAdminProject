using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vis.TestMode.Services.CRDWebAdmin
{
    public class ServerNamesHelper
    {
        private readonly AuthenticatedSession _authenticatedSession;

        public ServerNamesHelper(AuthenticatedSession authenticatedSession)
        {
            _authenticatedSession = authenticatedSession;
        }

        public async Task<List<string>> GetServerNamesAsync()
        {
            // Implement fetching logic for server names
            // For now, return a dummy list
            return new List<string> { "server1", "server2" };
        }
    }
}
