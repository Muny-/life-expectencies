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
    class Program
    {
        static void Main(string[] args)
        {
            LifeExpectancyAnalyzer analyzer = new LifeExpectancyAnalyzer("worldbank_life_expectancy_data.txt", "worldbank_life_expectancy_metadata.txt");

            analyzer.ConsumeData();

            string answer = prompt("Synopsis [0] or ranking [1] or growth [2] or drop [3] or plot [4] or plot_country [5]? ");

            switch(answer)
            {
                case "0":
                synopsis(analyzer);
                break;

                case "1":
                ranking(analyzer);
                break;

                case "2":
                growth(analyzer);
                break;

                case "3":
                drop(analyzer);
                break;

                case "4":
                plot(analyzer);
                break;

                case "5":
                plot_country(analyzer);
                break;
            }
        }

        static void plot_country(LifeExpectancyAnalyzer analyzer)
        {
            while(true)
            {
                Dictionary<string, Dictionary<int, double>> curves = new Dictionary<string, Dictionary<int, double>>();

                string entities_str = prompt("Enter entity name(s) or code(s) to plot (separated by commas, 'all' for every entity): ").Replace(", ", ",");

                if (entities_str.Trim() == "all")
                {
                    foreach(KeyValuePair<string, Entity> kvp in analyzer.Entities)
                    {
                        curves.Add(kvp.Value.Country_Name, kvp.Value.LifeExpectancyByYear);
                    }
                }
                else
                {
                    l("rawstr(", entities_str, ")");
                    string[] entities = entities_str.Split(",", StringSplitOptions.RemoveEmptyEntries);

                    l("Entities to plot: ", String.Join(":", entities));

                    Dictionary<string, Entity> results = analyzer.Entities.Where(val => entities.Contains(val.Key) || entities.Contains(val.Value.Country_Name)).ToDictionary(p => p.Key, p => p.Value);

                    if (results.Count < 1)
                    {
                        l("No such entities found");
                        continue;
                    }

                    foreach(string entity in entities)
                    {
                        curves.Add(entity, results.Where(val => val.Key == entity || val.Value.Country_Name == entity).First().Value.LifeExpectancyByYear);
                    }
                }

                generate_plot("country_plots", curves);
            }
        }

        static void plot(LifeExpectancyAnalyzer analyzer)
        {
            // Income categories

            Dictionary<string, Dictionary<int, double>> income_curves = new Dictionary<string, Dictionary<int, double>>();

            foreach(string income_group in analyzer.Income_Groups)
            {
                Dictionary<int, double> data = new Dictionary<int, double>();

                List<Dictionary<int, double>> country_expectancies = analyzer.Countries_Slash_Territories
                    .Where(p => p.Value.Income_group == income_group)
                    .ToDictionary(p => p.Key, p => p.Value).Values.ToList().Select(p => p.LifeExpectancyByYear).ToList();

                for(int i = 1960; i < 2015+1; i++)
                {
                    List<double> year_country_expectancy_data = new List<double>();

                    foreach(Dictionary<int, double> country_expectancy_set in country_expectancies)
                    {
                        if (country_expectancy_set.ContainsKey(i))
                            year_country_expectancy_data.Add(country_expectancy_set[i]);
                    }

                    year_country_expectancy_data = year_country_expectancy_data.OrderBy(p => p).ToList();

                    data.Add(i, year_country_expectancy_data.Median());
                }

                income_curves.Add(income_group, data);
            }

            generate_plot("income", income_curves);

            // Regions

            Dictionary<string, Dictionary<int, double>> region_curves = new Dictionary<string, Dictionary<int, double>>();

            foreach(string region in analyzer.Regions)
            {
                Dictionary<int, double> data = new Dictionary<int, double>();

                List<Dictionary<int, double>> country_expectancies = analyzer.Countries_Slash_Territories
                    .Where(p => p.Value.Region == region)
                    .ToDictionary(p => p.Key, p => p.Value).Values.ToList().Select(p => p.LifeExpectancyByYear).ToList();

                for(int i = 1960; i < 2015+1; i++)
                {
                    List<double> year_country_expectancy_data = new List<double>();

                    foreach(Dictionary<int, double> country_expectancy_set in country_expectancies)
                    {
                        if (country_expectancy_set.ContainsKey(i))
                            year_country_expectancy_data.Add(country_expectancy_set[i]);
                    }

                    year_country_expectancy_data = year_country_expectancy_data.OrderBy(p => p).ToList();

                    data.Add(i, year_country_expectancy_data.Median());
                }

                region_curves.Add(region, data);
            }

            generate_plot("region", region_curves);
        }

        static void generate_plot(string filename, Dictionary<string, Dictionary<int, double>> curves)
        {
            StringBuilder plot_data = new StringBuilder();

            plot_data.AppendLine("Year \"" + String.Join("\" \"", curves.Keys.ToArray()) + "\"");

            // assume all curves have the same number of datapoints as the first
            for(int i = 0; i < curves.Values.Max(val => val.Count); i++)
            {
                List<double> values = new List<double>();

                foreach(Dictionary<int, double> curve in curves.Values)
                {
                    if (i >= curve.Count)
                        break;

                    values.Add(curve.ElementAt(i).Value);
                }

                plot_data.AppendLine(curves.First().Value.ElementAt(i).Key + " " + String.Join(" ", values));
            }

            File.WriteAllText(filename + ".dat", plot_data.ToString());

            string gnuplot = @"set terminal wxt size 800,600 enhanced font 'Lato Light,12' persist

# Line width of the axes
set border linewidth 1.5
set key outside

plot for [col=2:" + (curves.Count+1) + @"] '" + filename + @".dat' using 1:col with lines title columnheader linewidth 2
    
pause -1 'press Ctrl-D to exit";

            // Write output plot to file
            File.WriteAllText(filename + ".gnu", gnuplot);

            // Display our results!
            ("gnuplot " + filename + ".gnu").Bash();
        }

        static void drop(LifeExpectancyAnalyzer analyzer)
        {
            
			DateTime start = DateTime.Now;

            Dictionary<string, Entity> filtered_by_insufficient_data = 
                analyzer.Countries_Slash_Territories.Where(p => p.Value.BiggestLifeExpectancyDrop != null).ToDictionary(p => p.Key, p => p.Value);

            List<Entity> sorted_worst_expectancy_drop = 
                filtered_by_insufficient_data
                    .OrderBy(
                        p => p.Value.BiggestLifeExpectancyDrop.Last().Value
                    ).ToDictionary(p => p.Key, p => p.Value).Values.ToList();

            l("Took: ", (DateTime.Now - start).TotalSeconds, " seconds");
        

            l("Worst life expectancy drops: 1960 to 2015");

            for(int i = 0; i < 10; i++)
            {
                l(
                    i+1, 
                    ": ", 
                    sorted_worst_expectancy_drop[i].Country_Name, 
                    " from ", 
                    sorted_worst_expectancy_drop[i].BiggestLifeExpectancyDrop.ElementAt(0).Key,
                    " (",
                    sorted_worst_expectancy_drop[i].BiggestLifeExpectancyDrop.ElementAt(0).Value,
                    ") to ",
                    sorted_worst_expectancy_drop[i].BiggestLifeExpectancyDrop.ElementAt(1).Key,
                    " (",
                    sorted_worst_expectancy_drop[i].BiggestLifeExpectancyDrop.ElementAt(1).Value,
                    "): ",
                    sorted_worst_expectancy_drop[i].BiggestLifeExpectancyDrop.ElementAt(1).Value - sorted_worst_expectancy_drop[i].BiggestLifeExpectancyDrop.ElementAt(0).Value
                );
            }
        }

        static void growth(LifeExpectancyAnalyzer analyzer)
        {
            while(true)
            {
                string str_starting_year_of_interest = prompt("Enter starting year of interest (-1 to quit): ");

                int starting_year_of_interest;

                if (!int.TryParse(str_starting_year_of_interest, out starting_year_of_interest))
                {
                    l("Error parsing starting year of interest, integers only please!");
                    continue;
                }

                if (starting_year_of_interest == -1)
                    break;

                string str_ending_year_of_interest = prompt("Enter ending year of interest (-1 to quit): ");

                int ending_year_of_interest;

                if (!int.TryParse(str_ending_year_of_interest, out ending_year_of_interest))
                {
                    l("Error parsing ending year of interest, integers only please!");
                    continue;
                }

                if (ending_year_of_interest == -1)
                    break;

                string user_input_region_name = prompt("Enter region (type 'all' to consider all): ");

                if (user_input_region_name != "all" && !analyzer.Regions.Contains(user_input_region_name))
                {
                    l("No such region!");
                    continue;
                }

                string user_input_income_category = prompt("Enter income category (type 'all' to consider all): ");

                if (user_input_income_category != "all" && !analyzer.Income_Groups.Contains(user_input_income_category))
                {
                    l("No such income category!");
                    continue;
                }

                Dictionary<string, Entity> countries_filtered_by_region = 
                    (user_input_region_name == "all" ? 
                        analyzer.Entities : 
                        analyzer.Entities_By_Region(user_input_region_name));

                Dictionary<string, Entity> countries_filtered_by_income_category = 
                    (user_input_income_category == "all" ? 
                        countries_filtered_by_region : 
                        countries_filtered_by_region.Intersect(
                            analyzer.Entities_By_Income_Group(user_input_income_category)
                        ).ToDictionary(k => k.Key, k => countries_filtered_by_region[k.Key]));

                Dictionary<string, Entity> countries_filtered_by_has_data = 
                    countries_filtered_by_income_category.Where(
                        p => p.Value.LifeExpectancyByYear.ContainsKey(starting_year_of_interest) &&
                             p.Value.LifeExpectancyByYear.ContainsKey(ending_year_of_interest)
                    ).ToDictionary(p => p.Key, p => p.Value);

                Dictionary<string, Entity> sorted_countries_dict = countries_filtered_by_has_data.OrderByDescending(
                    p => p.Value.LifeExpectancyByYear[ending_year_of_interest] - p.Value.LifeExpectancyByYear[starting_year_of_interest]
                ).ToDictionary(p => p.Key, p => p.Value);

                List<Entity> sorted_countries = sorted_countries_dict.Values.ToList();

                l("Top 10 Life Expectancy Growth: ", starting_year_of_interest, " to ", ending_year_of_interest);

                for(int i = 0; i < (sorted_countries.Count >= 10 ? 10 : sorted_countries.Count); i++)
                {
                    l(i+1, ": ", sorted_countries[i].Country_Name, " ", sorted_countries[i].LifeExpectancyByYear[ending_year_of_interest] - sorted_countries[i].LifeExpectancyByYear[starting_year_of_interest]);
                }

                l();

                l("Bottom 10 Life Expectancy Growth: ", starting_year_of_interest, " to ", ending_year_of_interest);

                for(int i = sorted_countries.Count-1; i > (sorted_countries.Count-1)-(sorted_countries.Count >= 10 ? 10 : sorted_countries.Count); i--)
                {
                    l(i+1, ": ", sorted_countries[i].Country_Name, " ", sorted_countries[i].LifeExpectancyByYear[ending_year_of_interest] - sorted_countries[i].LifeExpectancyByYear[starting_year_of_interest]);
                }
            }
        }

        static void ranking(LifeExpectancyAnalyzer analyzer)
        {
            while(true)
            {
                string str_year_of_interest = prompt("Enter year of interest (-1 to quit): ");

                int year_of_interest;

                if (!int.TryParse(str_year_of_interest, out year_of_interest))
                {
                    l("Error parsing year of interest, integers only please!");
                    continue;
                }

                if (year_of_interest == -1)
                    break;

                string user_input_region_name = prompt("Enter region (type 'all' to consider all): ");

                if (user_input_region_name != "all" && !analyzer.Regions.Contains(user_input_region_name))
                {
                    l("No such region!");
                    continue;
                }

                string user_input_income_category = prompt("Enter income category (type 'all' to consider all): ");

                if (user_input_income_category != "all" && !analyzer.Income_Groups.Contains(user_input_income_category))
                {
                    l("No such income category!");
                    continue;
                }

                Dictionary<string, Entity> countries_filtered_by_region = 
                    (user_input_region_name == "all" ? 
                        analyzer.Entities : 
                        analyzer.Entities_By_Region(user_input_region_name));

                Dictionary<string, Entity> countries_filtered_by_income_category = 
                    (user_input_income_category == "all" ? 
                        countries_filtered_by_region : 
                        countries_filtered_by_region.Intersect(
                            analyzer.Entities_By_Income_Group(user_input_income_category)
                        ).ToDictionary(k => k.Key, k => countries_filtered_by_region[k.Key]));

                Dictionary<string, Entity> countries_filtered_by_has_data = countries_filtered_by_income_category.Where(p => p.Value.LifeExpectancyByYear.ContainsKey(year_of_interest)).ToDictionary(p => p.Key, p => p.Value);

                Dictionary<string, Entity> sorted_countries_dict = countries_filtered_by_has_data.OrderByDescending(
                    p => p.Value.LifeExpectancyByYear[year_of_interest]
                ).ToDictionary(p => p.Key, p => p.Value);

                List<Entity> sorted_countries = sorted_countries_dict.Values.ToList();

                l("Top 10 Life Expectancy for ", year_of_interest);

                for(int i = 0; i < (sorted_countries.Count >= 10 ? 10 : sorted_countries.Count); i++)
                {
                    l(i+1, ": ", sorted_countries[i].Country_Name, " ", sorted_countries[i].LifeExpectancyByYear[year_of_interest]);
                }

                l();

                l("Bottom 10 Life Expectancy for ", year_of_interest);

                for(int i = sorted_countries.Count-1; i > (sorted_countries.Count-1)-(sorted_countries.Count >= 10 ? 10 : sorted_countries.Count); i--)
                {
                    l(i+1, ": ", sorted_countries[i].Country_Name, " ", sorted_countries[i].LifeExpectancyByYear[year_of_interest]);
                }
            }
        }

        static void synopsis(LifeExpectancyAnalyzer analyzer)
        {
            l("Total number of entities: ", analyzer.Entities.Count);
            l("Number of countries/territories: ", analyzer.Countries_Slash_Territories.Count);

            l();

            l("Regions and their country count:");

            foreach(string region in analyzer.Regions)
            {
                l(region, ": ", analyzer.Entities_By_Region(region).Count);
            }

            l();

            l("Income categories and their country count:");

            foreach(string income_group in analyzer.Income_Groups)
            {
                l(income_group, ": ", analyzer.Entities_By_Income_Group(income_group).Count);
            }

            l();

            while(true)
            {
                string user_input_region_name = prompt("Enter region name: ");

                if (analyzer.Regions.Contains(user_input_region_name))
                {
                    l("Countries in the '", user_input_region_name, "' region: ");

                    foreach(KeyValuePair<string, Entity> kvp in analyzer.Entities_By_Region(user_input_region_name))
                    {
                        l(kvp.Value.Country_Name, " (", kvp.Value.Country_Code, ")");
                    }

                    break;
                }
                else
                {
                    l("Unknown region name, the available options are: ", String.Join(", ", analyzer.Regions));
                }
            }

            l();

            while(true)
            {
                string user_input_income_category = prompt("Enter income category: ");

                if (analyzer.Income_Groups.Contains(user_input_income_category))
                {
                    l("Countries in the '", user_input_income_category, "' income category: ");

                    foreach(KeyValuePair<string, Entity> kvp in analyzer.Entities_By_Income_Group(user_input_income_category))
                    {
                        l(kvp.Value.Country_Name, " (", kvp.Value.Country_Code, ")");
                    }

                    break;
                }
                else
                {
                    l("Unknown income category, the available options are: ", String.Join(", ", analyzer.Income_Groups));
                }
            }

            l();

            while(true)
            {
                string user_input_country_or_country_code = prompt("Enter name of country or country code (Enter to quit): ");
                
                if (user_input_country_or_country_code == "")
                    break;

                Dictionary<string, Entity> results = analyzer.Entities.Where(val => val.Value.Country_Code == user_input_country_or_country_code || val.Value.Country_Name == user_input_country_or_country_code).ToDictionary(p => p.Key, p => p.Value);

                if (results.Count > 0)
                {
                    l("Data for ", user_input_country_or_country_code, ":");

                    foreach(KeyValuePair<int, double> kvp in results.First().Value.LifeExpectancyByYear)
                    {
                        l("Year: ", kvp.Key, "  Life expectancy: ", kvp.Value);
                    }
                }
                else
                {
                    l("Unknown country/country code, available options are: ", String.Join(", ", Array.ConvertAll<Entity, string>(analyzer.Entities.Values.ToArray(), Convert.ToString)));
                }
            }
        }

        static void l(params dynamic[] msgs)
        {
            Console.WriteLine(String.Join("", Array.ConvertAll<dynamic, string>(msgs, Convert.ToString)));
        }

        static string prompt(string msg)
        {
            Console.Write(msg);

            return Console.ReadLine();
        }
    }
}
