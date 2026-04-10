using BusinessLayer.Enum;
using BusinessLayer.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BusinessLayer.Services
{
    public class PlatformManager
    {
        private readonly IEnumerable<IVirtualizationAdapter> _adapters;

        public PlatformManager(IEnumerable<IVirtualizationAdapter> adapters)
        { 
            _adapters = adapters;
        }
        //maybe alternative method for string from database?
        public IVirtualizationAdapter GetAdapter(PlatformType platformType)
        {
            // Najdeme ten adaptér, jehož název se shoduje s typem platformy v databázi
            var targetAdapter = _adapters.FirstOrDefault(a => a.PlatformName == platformType);

            if (targetAdapter == null)
            {
                throw new Exception($"Neznámá nebo nepodporovaná platforma: {platformType}");
            }

            return targetAdapter;
        }
    }
}
