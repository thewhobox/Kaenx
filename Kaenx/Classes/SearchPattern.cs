using Kaenx.Konnect.Addresses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kaenx.Classes
{
    public class SearchPattern
    {
        public string Areas { get; set; }
        public string Lines { get; set; }
        public string Devices { get; set; }

        public int Count
        {
            get
            {
                return GetAddresses().Count;
            }
        }


        public List<UnicastAddress> GetAddresses()
        {
            List<UnicastAddress> addresses = new List<UnicastAddress>();
                
                foreach (int area in GetNumbs(Areas))
                {
                    foreach (int line in GetNumbs(Lines))
                    {
                        foreach (int dev in GetNumbs(Devices))
                        {
                            addresses.Add(new UnicastAddress((byte)area, (byte)line, (byte)dev));
                        }
                    }
                }

            return addresses;
        }

        private List<int> GetNumbs(string input)
        {
            List<int> numbs = new List<int>();
            string[] _numbs = input.Split(",");
            foreach (string numb in _numbs)
            {
                if (numb.Contains("-"))
                {
                    string[] numbsx = numb.Split("-");
                    int from = int.Parse(numbsx[0]);
                    int to = int.Parse(numbsx[1]);
                    for (int i = from; i <= to; i++)
                        numbs.Add(i);
                }
                else
                {
                    int a;
                    if (int.TryParse(numb, out a))
                        numbs.Add(a);
                }
            }
            return numbs;
        }
    }
}
