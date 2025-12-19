using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary9.Models
{
    /// <summary>
    /// Информация о проеме в стене
    /// </summary>
    public class OpeningInfo
    {
       
        public string WallName { get; set; }
  
        public string WallType { get; set; }
     
        public double Lenght { get; set; }
        public double Height { get; set; }
        public double Thickness { get; set; }
        public double Volume { get; set; }
        public double Area { get; set; }
        public bool IsCorrect { get; set; }
        
    }
}
