using System;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Numerics;

namespace src
{
    public class LifeExpectancyAnalyzer
    {
        private string Data_File;
        private string Metadata_File;

        public Dictionary<string, Entity> Entities = new Dictionary<string, Entity>();

        private Dictionary<string, Entity> _countries_slash_territories;

        public Dictionary<string, Entity> Countries_Slash_Territories
        {
            get
            {
                if (_countries_slash_territories == null)
                    _countries_slash_territories = Entities.Where(val => val.Value.Region != "").ToDictionary(p => p.Key, p => p.Value);

                return _countries_slash_territories;
            }
        }

        private List<string> _regions;

        public List<string> Regions
        {
            get
            {
                if (_regions == null)
                {
                    _regions = new List<string>();

                    foreach (KeyValuePair<string, Entity> kvp in Entities)
                    {
                        if (kvp.Value.Region != "" && !_regions.Contains(kvp.Value.Region))
                            _regions.Add(kvp.Value.Region);
                    }
                }

                return _regions;
            }
        }

        public Dictionary<string, Entity> Entities_By_Region(string region)
        {
            return Entities.Where(val => val.Value.Region == region).ToDictionary(p => p.Key, p => p.Value);
        }

        private List<string> _income_groups;

        public List<string> Income_Groups
        {
            get
            {
                if (_income_groups == null)
                {
                    _income_groups = new List<string>();

                    foreach (KeyValuePair<string, Entity> kvp in Entities)
                    {
                        if (kvp.Value.Income_group != "" && !_income_groups.Contains(kvp.Value.Income_group))
                            _income_groups.Add(kvp.Value.Income_group);
                    }
                }

                return _income_groups;
            }
        }

        public Dictionary<string, Entity> Entities_By_Income_Group(string income_group)
        {
            return Entities.Where(val => val.Value.Income_group == income_group).ToDictionary(p => p.Key, p => p.Value);
        }

        public LifeExpectancyAnalyzer(string data_file, string metadata_file)
        {
            this.Data_File = data_file;
            this.Metadata_File = metadata_file;
        }

        public void ConsumeData()
        {
            string[] expectancy_data = File.ReadAllLines(Data_File);

            string[] expectancy_metadata = File.ReadAllLines(Metadata_File);

            for(int i = 1; i < expectancy_data.Length; i++)
            {
                string[] data_parts = expectancy_data[i].Split(",", StringSplitOptions.None);
                string[] metadata_parts = expectancy_metadata[i].Split(",", StringSplitOptions.None);

                string country_name = data_parts[0];
                string country_code = data_parts[1];

                Dictionary<int, double> life_expectancy_by_year = new Dictionary<int, double>();

                for(int yr = 2; yr < data_parts.Length; yr++)
                {
                    double expectancy;

                    double.TryParse(data_parts[yr], out expectancy);

                    if (expectancy != 0)
                        life_expectancy_by_year.Add(1960 - 2 + yr, expectancy);
                }

                string region = metadata_parts[1];
                string income_group = metadata_parts[2];
                string special_notes = metadata_parts[3];

                Entities.Add(country_code, new Entity(country_name, country_code, life_expectancy_by_year, region, income_group, special_notes));
            }
        }
    }
}