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
	public class Entity
	{
		public string Country_Name;

		public string Country_Code;

		public string Region;

		public string Income_group;

		public string Special_notes;

		public Dictionary<int, double> LifeExpectancyByYear = new Dictionary<int, double>();

		private Dictionary<int, double> _biggest_life_expectancy_drop;

		public Dictionary<int, double> BiggestLifeExpectancyDrop
		{
			get
			{
				if (_biggest_life_expectancy_drop == null)
				{
					if (LifeExpectancyByYear.Count < 2)
						return null;

					Dictionary<List<int>, double> deltas = new Dictionary<List<int>, double>();

					for(int i = 0; i < LifeExpectancyByYear.Count; i++)
					{
						if (i != LifeExpectancyByYear.Count-1)
						{
							for(int y = i+1; y < LifeExpectancyByYear.Count; y++)
							{
								double delta = LifeExpectancyByYear.ElementAt(y).Value - LifeExpectancyByYear.ElementAt(i).Value;

								deltas.Add(new List<int>() {LifeExpectancyByYear.ElementAt(i).Key, LifeExpectancyByYear.ElementAt(y).Key}, delta);
							}
						}
					}

					deltas = deltas.OrderBy(p => p.Value).ToDictionary(p => p.Key, p => p.Value);

					_biggest_life_expectancy_drop = new Dictionary<int, double>() {
						{ deltas.First().Key[0], LifeExpectancyByYear[deltas.First().Key[0]] },
						{ deltas.First().Key[1], LifeExpectancyByYear[deltas.First().Key[1]] },
						{ deltas.First().Key[1] - deltas.First().Key[0], LifeExpectancyByYear[deltas.First().Key[1]] - LifeExpectancyByYear[deltas.First().Key[0]]}
					};
				}

				return _biggest_life_expectancy_drop;
			}
		}

		public Entity(string name, string c_code, Dictionary<int, double> expectancybyyear, string region, string income_group, string special_notes)
		{
			this.Country_Name = name;
			this.Country_Code = c_code;

			this.Region = region;
			this.Income_group = income_group;
			this.Special_notes = special_notes;

			LifeExpectancyByYear = expectancybyyear;
		}
	}
}