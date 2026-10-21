// Венгерский алгоритм задачи о назначениях.
// Пять шагов по Грешилову А. А., «Математические методы принятия
// решений», 2-е изд., 2014, § 3.5, с. 150–155.
namespace Hungarian;

public static class Program
{
    static readonly int[,] C =
    {
        { 2, 10, 9, 7 },
        { 15, 4, 14, 8 },
        { 13, 14, 16, 11 },
        { 4, 15, 13, 19 },
    };

    // Печать матрицы; выбранные ячейки — в скобках.
    static void Show(string title, int[,] m, ICollection<(int, int)>? marked = null)
    {
        Console.WriteLine($"\n{title}");
        for (int i = 0; i < m.GetLength(0); i++)
        {
            var cells = Enumerable.Range(0, m.GetLength(1)).Select(j =>
                marked != null && marked.Contains((i, j)) ? $"[{m[i, j]}]" : $" {m[i, j]} ");
            Console.WriteLine(" " + string.Join(" ", cells.Select(s => s.PadLeft(5))));
        }
    }

    // Шаг 3: максимальное число независимых нулей. Увеличивающие цепи
    // (алгоритм Куна) в двудольном графе «строка — столбец с нулём».
    static Dictionary<int, int> TryAssign(int[,] d)
    {
        int n = d.GetLength(0);
        var matchCol = new Dictionary<int, int>(); // столбец -> строка

        bool Dfs(int i, HashSet<int> seen)
        {
            for (int j = 0; j < n; j++)
                if (d[i, j] == 0 && seen.Add(j)
                    && (!matchCol.ContainsKey(j) || Dfs(matchCol[j], seen)))
                {
                    matchCol[j] = i;
                    return true;
                }
            return false;
        }

        for (int i = 0; i < n; i++)
            Dfs(i, new HashSet<int>());
        return matchCol.ToDictionary(kv => kv.Value, kv => kv.Key); // строка -> столбец
    }

    // Шаг 4а: минимальное покрытие нулей линиями (теорема Кёнига).
    // Помечаем строки без назначения; из них — столбцы с нулями;
    // из столбцов — строки, назначенные на них; до стабилизации.
    // Линии = непомеченные строки + помеченные столбцы.
    static (HashSet<int> Rows, HashSet<int> Cols) MinCover(int[,] d, Dictionary<int, int> assign)
    {
        int n = d.GetLength(0);
        var markedRows = Enumerable.Range(0, n).Where(i => !assign.ContainsKey(i)).ToHashSet();
        var markedCols = new HashSet<int>();
        bool changed = true;
        while (changed)
        {
            changed = false;
            foreach (int i in markedRows.ToList())
                for (int j = 0; j < n; j++)
                    if (d[i, j] == 0 && markedCols.Add(j))
                        changed = true;
            foreach (var (i, j) in assign)
                if (markedCols.Contains(j) && markedRows.Add(i))
                    changed = true;
        }
        return (Enumerable.Range(0, n).Where(i => !markedRows.Contains(i)).ToHashSet(), markedCols);
    }

    public static (int[] Assign, int Total) Solve(int[,] c)
    {
        int n = c.GetLength(0);
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
            if (assign.Count == n) break;
            Console.WriteLine($"   независимых нулей {assign.Count} < {n} — неполное");
            // Шаг 4. Модификация редуцированной матрицы
            var (rows, cols) = MinCover(d, assign);
            Console.WriteLine($"   линии: строки [{string.Join(", ", rows.OrderBy(i => i).Select(i => i + 1))}], "
                + $"столбцы [{string.Join(", ", cols.OrderBy(j => j).Select(j => j + 1))}]");
            int theta = (from i in Enumerable.Range(0, n) from j in Enumerable.Range(0, n)
                         where !rows.Contains(i) && !cols.Contains(j) select d[i, j]).Min();
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    if (!rows.Contains(i) && !cols.Contains(j)) d[i, j] -= theta;
                    else if (rows.Contains(i) && cols.Contains(j)) d[i, j] += theta;
            bound += theta * (n - rows.Count - cols.Count);
            Show($"Шаг 4. Модификация, θ = {theta}", d);
            Console.WriteLine($"   нижняя оценка: {bound}");
        }

        // Шаг 5. Проведение назначений (стоимость — по исходной матрице)
        var res = Enumerable.Range(0, n).Select(i => assign[i]).ToArray();
        int total = Enumerable.Range(0, n).Sum(i => c[i, res[i]]);
        Console.WriteLine("\nШаг 5. Оптимальное назначение");
        for (int i = 0; i < n; i++)
            Console.WriteLine($"   датчик {i + 1} -> объект {res[i] + 1}   c = {c[i, res[i]]}");
        Console.WriteLine($"   f(x) = {string.Join(" + ", res.Select((j, i) => c[i, j]))} = {total}");
        return (res, total);
    }

    public static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        int[,] c = args.Length > 0 ? ReadMatrix(args[0]) : C;
        Show("Исходная матрица C", c);
        var (_, total) = Solve(c);
        // Контроль полным перебором всех n! планов
        int n = c.GetLength(0);
        int best = Permutations(Enumerable.Range(0, n).ToArray())
            .Min(p => p.Select((j, i) => c[i, j]).Sum());
        Console.WriteLine($"\nПроверка перебором: минимум = {best} "
            + (best == total ? "— совпадает" : "— РАСХОЖДЕНИЕ"));
    }

    // Все перестановки чисел — для контроля перебором.
    static IEnumerable<int[]> Permutations(int[] items) =>
        items.Length == 0
            ? new[] { Array.Empty<int>() }
            : items.SelectMany(x => Permutations(items.Where(y => y != x).ToArray()),
                               (x, rest) => rest.Prepend(x).ToArray());

    // Чтение квадратной матрицы из файла: строка — датчик, числа через пробел.
    static int[,] ReadMatrix(string path)
    {
        var rows = File.ReadLines(path).Where(l => l.Trim() != "")
            .Select(l => l.Split(' ', '\t', ';', ',').Where(s => s != "").Select(int.Parse).ToArray())
            .ToArray();
        if (rows.Any(r => r.Length != rows.Length))
            throw new FormatException(
                "Матрица должна быть квадратной; для прямоугольной добавьте фиктивные строки или столбцы с нулями.");
        var m = new int[rows.Length, rows.Length];
        for (int i = 0; i < rows.Length; i++)
            for (int j = 0; j < rows.Length; j++)
                m[i, j] = rows[i][j];
        return m;
    }
}
