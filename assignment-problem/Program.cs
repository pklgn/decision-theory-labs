// Венгерский алгоритм задачи о назначениях (задача 14: 4 датчика на 4 объекта).
// Пять шагов по Грешилову А. А., «Математические методы принятия
// решений», 2-е изд., 2014, § 3.5, с. 150–155.
// Порт листинга hungarian.py из презентации; вывод совпадает со слайдом 26.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Hungarian
{
    public static class Program
    {
        // Строка — датчик, столбец — объект, число — цена размещения.
        static readonly int[,] C =
        {
            { 2, 10, 9, 7 },
            { 15, 4, 14, 8 },
            { 13, 14, 16, 11 },
            { 4, 15, 13, 19 },
        };

        /// <summary>Печать матрицы; выбранные ячейки — в скобках.</summary>
        static void Show(string title, int[,] m, ICollection<(int, int)>? marked = null)
        {
            Console.WriteLine();
            Console.WriteLine(title);
            int n = m.GetLength(0);
            for (int i = 0; i < n; i++)
            {
                var cells = new List<string>();
                for (int j = 0; j < m.GetLength(1); j++)
                {
                    string c = marked != null && marked.Contains((i, j)) ? $"[{m[i, j]}]" : $" {m[i, j]} ";
                    cells.Add(c.PadLeft(5));
                }
                Console.WriteLine(" " + string.Join(" ", cells));
            }
        }

        /// <summary>
        /// Шаг 3: максимальное число независимых нулей.
        /// Увеличивающие цепи (алгоритм Куна) в двудольном графе
        /// «строка — столбец с нулём». Возвращает {строка: столбец}.
        /// </summary>
        static Dictionary<int, int> TryAssign(int[,] d)
        {
            int n = d.GetLength(0);
            var matchCol = new Dictionary<int, int>(); // столбец -> строка

            bool Dfs(int i, HashSet<int> seen)
            {
                for (int j = 0; j < n; j++)
                {
                    if (d[i, j] == 0 && !seen.Contains(j))
                    {
                        seen.Add(j);
                        if (!matchCol.ContainsKey(j) || Dfs(matchCol[j], seen))
                        {
                            matchCol[j] = i;
                            return true;
                        }
                    }
                }
                return false;
            }

            for (int i = 0; i < n; i++)
                Dfs(i, new HashSet<int>());
            return matchCol.ToDictionary(kv => kv.Value, kv => kv.Key);
        }

        /// <summary>
        /// Шаг 4а: минимальное покрытие нулей линиями (теорема Кёнига).
        /// Помечаем строки без назначения; из них — столбцы с нулями;
        /// из столбцов — строки, назначенные на них; до стабилизации.
        /// Линии = непомеченные строки + помеченные столбцы.
        /// </summary>
        static (HashSet<int> Rows, HashSet<int> Cols) MinCover(int[,] d, Dictionary<int, int> assign)
        {
            int n = d.GetLength(0);
            var markedRows = new HashSet<int>(Enumerable.Range(0, n).Where(i => !assign.ContainsKey(i)));
            var markedCols = new HashSet<int>();
            bool changed = true;
            while (changed)
            {
                changed = false;
                foreach (int i in markedRows.ToList())
                    for (int j = 0; j < n; j++)
                        if (d[i, j] == 0 && markedCols.Add(j))
                            changed = true;
                foreach (var (i, j) in assign.Select(kv => (kv.Key, kv.Value)))
                    if (markedCols.Contains(j) && markedRows.Add(i))
                        changed = true;
            }
            var lineRows = new HashSet<int>(Enumerable.Range(0, n).Where(i => !markedRows.Contains(i)));
            return (lineRows, markedCols);
        }

        public static (int[] Assign, int Total) Solve(int[,] c)
        {
            int n = c.GetLength(0);
            if (c.GetLength(1) != n)
                throw new ArgumentException("Матрица должна быть квадратной (для прямоугольной добавьте фиктивные строки или столбцы с нулями).");
            var d = (int[,])c.Clone();
            int bound = 0;

            // Шаг 1. Редукция строк
            for (int i = 0; i < n; i++)
            {
                int m = Enumerable.Range(0, n).Min(j => d[i, j]);
                bound += m;
                for (int j = 0; j < n; j++) d[i, j] -= m;
            }
            Show("Шаг 1. Редукция строк", d);

            // Шаг 2. Редукция столбцов
            for (int j = 0; j < n; j++)
            {
                int m = Enumerable.Range(0, n).Min(i => d[i, j]);
                bound += m;
                for (int i = 0; i < n; i++) d[i, j] -= m;
            }
            Show("Шаг 2. Редукция столбцов", d);
            Console.WriteLine($"   нижняя оценка: {bound}");

            Dictionary<int, int> assign;
            while (true)
            {
                // Шаг 3. Определение назначений
                assign = TryAssign(d);
                Show("Шаг 3. Назначения по нулям", d, assign.Select(kv => (kv.Key, kv.Value)).ToList());
                if (assign.Count == n)
                    break;
                Console.WriteLine($"   независимых нулей {assign.Count} < {n} — неполное");

                // Шаг 4. Модификация редуцированной матрицы
                var (rows, cols) = MinCover(d, assign);
                Console.WriteLine($"   линии: строки [{string.Join(", ", rows.OrderBy(i => i).Select(i => i + 1))}], " +
                                  $"столбцы [{string.Join(", ", cols.OrderBy(j => j).Select(j => j + 1))}]");
                int theta = int.MaxValue;
                for (int i = 0; i < n; i++)
                    for (int j = 0; j < n; j++)
                        if (!rows.Contains(i) && !cols.Contains(j))
                            theta = Math.Min(theta, d[i, j]);
                for (int i = 0; i < n; i++)
                    for (int j = 0; j < n; j++)
                    {
                        if (!rows.Contains(i) && !cols.Contains(j))
                            d[i, j] -= theta;
                        else if (rows.Contains(i) && cols.Contains(j))
                            d[i, j] += theta;
                    }
                bound += theta * (n - rows.Count - cols.Count);
                Show($"Шаг 4. Модификация, θ = {theta}", d);
                Console.WriteLine($"   нижняя оценка: {bound}");
            }

            // Шаг 5. Проведение назначений (стоимость — по исходной матрице)
            var result = Enumerable.Range(0, n).Select(i => assign[i]).ToArray();
            int total = Enumerable.Range(0, n).Sum(i => c[i, result[i]]);
            Console.WriteLine();
            Console.WriteLine("Шаг 5. Оптимальное назначение");
            for (int i = 0; i < n; i++)
                Console.WriteLine($"   датчик {i + 1} -> объект {result[i] + 1}   c = {c[i, result[i]]}");
            string terms = string.Join(" + ", Enumerable.Range(0, n).Select(i => c[i, result[i]]));
            Console.WriteLine($"   f(x) = {terms} = {total}");
            return (result, total);
        }

        /// <summary>Контроль полным перебором всех n! планов.</summary>
        static int BruteForceMin(int[,] c)
        {
            int n = c.GetLength(0);
            int best = int.MaxValue;
            var perm = Enumerable.Range(0, n).ToArray();
            var used = new bool[n];

            void Go(int i, int sum)
            {
                if (i == n) { best = Math.Min(best, sum); return; }
                for (int j = 0; j < n; j++)
                {
                    if (used[j]) continue;
                    used[j] = true;
                    Go(i + 1, sum + c[i, j]);
                    used[j] = false;
                }
            }

            Go(0, 0);
            return best;
        }

        /// <summary>Чтение матрицы из файла: строка — датчик, числа через пробел.</summary>
        static int[,] ReadMatrix(string path)
        {
            var rows = File.ReadAllLines(path)
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Select(l => l.Split(new[] { ' ', '\t', ';', ',' }, StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray())
                .ToArray();
            var m = new int[rows.Length, rows[0].Length];
            for (int i = 0; i < rows.Length; i++)
                for (int j = 0; j < rows[i].Length; j++)
                    m[i, j] = rows[i][j];
            return m;
        }

        public static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            int[,] c = args.Length > 0 ? ReadMatrix(args[0]) : C;

            Show("Исходная матрица C", c);
            var (_, total) = Solve(c);

            int best = BruteForceMin(c);
            Console.WriteLine();
            Console.WriteLine($"Проверка перебором: минимум = {best} " + (best == total ? "— совпадает" : "— РАСХОЖДЕНИЕ"));
        }
    }
}
