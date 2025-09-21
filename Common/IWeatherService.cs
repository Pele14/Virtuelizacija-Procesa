using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
  
    
        [ServiceContract]
        public interface IWeatherService
        {
            [OperationContract]
            string StartSession(string meta);

            [OperationContract]
            string PushSample(WeatherSample sample);

            [OperationContract]
            string EndSession();
        }
    }

