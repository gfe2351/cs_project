using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Data.Sqlite;

namespace LibraryProject
{
    // ==========================================
    // КЛАСС КНИГИ (Задачи 1, 3, 7, 8)
    // ==========================================
    public class Book
    {
        public int Id { get; set; }
        
        // Задача 1: Поля Title, Author, Year
        public string Title;
        public string Author;
        public int Year;

        public bool Available;
        public string? BorrowedBy;

        public Book(string title, string author, int year, bool available = true, string? borrowedBy = null, int id = 0)
        {
            Id = id;
            Title = title;
            Author = author;
            Year = year;
            Available = available;
            BorrowedBy = borrowedBy;
        }

        // Задача 1: Метод DisplayInfo(), который выводит информацию о книге
        public void DisplayInfo()
        {
            string status = Available ? "Доступна" : $"Выдана читателю: {BorrowedBy}";
            Console.WriteLine($"Книга: \"{Title}\" | Автор: {Author} | Год: {Year} |Возраст: {CalculateBookAge(Year)} лет | Статус: {status}");
        }

        // Задача 3: Метод с одним параметром SetTitle(string title), изменяющий название книги
        public void SetTitle(string title)
        {
            Title = title;
        }

        // Задача 7: Метод проверки IsBookAvailable(), возвращающий true, если книга доступна, и false, если выдана
        public bool IsBookAvailable()
        {
            return Available;
        }

        // Задача 8: Метод расчёта CalculateBookAge(int year), возвращающий возраст книги
        public static int CalculateBookAge(int year)
        {
            int currentYear = DateTime.Now.Year;
            return Math.Max(0, currentYear - year);
        }
    }

    // ==========================================
    // КЛАСС ЧИТАТЕЛЯ
    // ==========================================
    public class Reader
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string? Phone { get; set; }

        public Reader(string name, string? phone = null, int id = 0)
        {
            Id = id;
            Name = name;
            Phone = phone;
        }

        public void DisplayReader()
        {
            string phoneInfo = string.IsNullOrWhiteSpace(Phone) ? "не указан" : Phone;
            Console.WriteLine($"Читатель: {Name} | Телефон: {phoneInfo}");
        }
    }

    // ==========================================
    // КЛАСС БИБЛИОТЕКИ (Задачи 2, 4, 6–24)
    // ==========================================
    public class Library
    {
        private readonly string _connectionString = "Data Source=library.db";
        private readonly List<Book> _books = new List<Book>();
        private readonly List<Reader> _readers = new List<Reader>();

        public static string LibraryName = "Центральная Городская Библиотека";

        // Задача 15: Статическое поле TotalBooks
        public static int TotalBooks = 0;

        public Library()
        {
            InitializeDatabase();
            LoadDataFromDatabase();
        }

        // Инициализация таблиц SQLite
        private void InitializeDatabase()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Books (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Title TEXT NOT NULL,
                    Author TEXT NOT NULL,
                    Year INTEGER NOT NULL,
                    Available INTEGER NOT NULL DEFAULT 1,
                    BorrowedBy TEXT
                );
                CREATE TABLE IF NOT EXISTS Readers (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Phone TEXT
                );";
            cmd.ExecuteNonQuery();
        }

        // Загрузка данных из SQLite в списки при старте
        private void LoadDataFromDatabase()
        {
            _books.Clear();
            _readers.Clear();
            TotalBooks = 0;

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT Id, Title, Author, Year, Available, BorrowedBy FROM Books";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    int id = reader.GetInt32(0);
                    string title = reader.GetString(1);
                    string author = reader.GetString(2);
                    int year = reader.GetInt32(3);
                    bool available = reader.GetInt32(4) == 1;
                    string? borrowedBy = reader.IsDBNull(5) ? null : reader.GetString(5);

                    _books.Add(new Book(title, author, year, available, borrowedBy, id));
                    IncrementTotalBooks();
                }
            }

            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = "SELECT Id, Name, Phone FROM Readers";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    int id = reader.GetInt32(0);
                    string name = reader.GetString(1);
                    string? phone = reader.IsDBNull(2) ? null : reader.GetString(2);

                    _readers.Add(new Reader(name, phone, id));
                }
            }
        }

        // Задача 2: Метод без параметров ShowWelcomeMessage()
        public void ShowWelcomeMessage()
        {
            Console.WriteLine("Добро пожаловать в автоматизированную систему управления библиотекой!");
        }

        // Задача 11: Статический метод ShowLibraryName()
        public static void ShowLibraryName()
        {
            Console.WriteLine($"Библиотека: {LibraryName}");
        }

        // Задача 15: Метод, который увеличивает значение статического счётчика TotalBooks
        public static void IncrementTotalBooks()
        {
            TotalBooks++;
        }

        // Задача 4: Метод с несколькими параметрами AddBook(string title, string author, int year)
        public void AddBook(string title, string author, int year)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Books (Title, Author, Year, Available, BorrowedBy)
                VALUES ($title, $author, $year, 1, NULL);
                SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("$title", title);
            cmd.Parameters.AddWithValue("$author", author);
            cmd.Parameters.AddWithValue("$year", year);

            int newId = Convert.ToInt32(cmd.ExecuteScalar());
            var book = new Book(title, author, year, true, null, newId);
            _books.Add(book);

            // Задача 15: Увеличение статического счётчика при добавлении новой книги
            IncrementTotalBooks();

            Console.WriteLine("Книга успешно добавлена!");
        }

        // Обновление названия книги в БД (для Задачи 3)
        public bool RenameBook(string oldTitle, string newTitle)
        {
            Book? book = FindBook(oldTitle);
            if (book == null) return false;

            book.SetTitle(newTitle);

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var cmd = connection.CreateCommand();
            cmd.CommandText = "UPDATE Books SET Title = $newTitle WHERE Id = $id";
            cmd.Parameters.AddWithValue("$newTitle", newTitle);
            cmd.Parameters.AddWithValue("$id", book.Id);
            cmd.ExecuteNonQuery();

            return true;
        }

        // Задача 6: Метод GetBookCount(), возвращающий количество книг в библиотеке
        public int GetBookCount()
        {
            return _books.Count;
        }

        // Задача 7: Проверка доступности книги по названию
        public bool IsBookAvailable(string title)
        {
            Book? book = FindBook(title);
            return book != null && book.IsBookAvailable();
        }

        // Задача 9: Метод поиска FindBook(string title), возвращающий результат поиска
        public Book? FindBook(string title)
        {
            return _books.FirstOrDefault(b => b.Title.Equals(title, StringComparison.OrdinalIgnoreCase));
        }

        // Задача 10: Метод проверки читателя IsReaderRegistered(string name)
        public bool IsReaderRegistered(string name)
        {
            return _readers.Any(r => r.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        // Задача 12: Нестатический метод ShowBooks(), выводящий список книг объекта библиотеки
        public void ShowBooks()
        {
            if (_books.Count == 0)
            {
                Console.WriteLine("В библиотеке пока нет книг.");
                return;
            }

            Console.WriteLine("\n--- Список книг в библиотеке ---");
            foreach (var book in _books)
            {
                book.DisplayInfo();
            }
        }

        public void ShowReaders()
        {
            if (_readers.Count == 0)
            {
                Console.WriteLine("Нет зарегистрированных читателей.");
                return;
            }

            Console.WriteLine("\n--- Зарегистрированные читатели ---");
            foreach (var reader in _readers)
            {
                reader.DisplayReader();
            }
        }

        // Задача 13: Статический метод расчёта штрафа CalculateFine(int overdueDays)
        public static decimal CalculateFine(int overdueDays)
        {
            if (overdueDays <= 0) return 0m;
            decimal ratePerDay = 100m; // 100 тенге за каждый день просрочки
            return overdueDays * ratePerDay;
        }

        // Задача 14: Статический метод GetMaxBooks(int first, int second)
        public static int GetMaxBooks(int first, int second)
        {
            return Math.Max(first, second);
        }

        // Задача 14: Нестатический метод GetAvailableBooks()
        public List<Book> GetAvailableBooks()
        {
            return _books.Where(b => b.IsBookAvailable()).ToList();
        }

        // Задача 16: Перегрузка методов SearchBook() (1 - поиск по названию)
        public List<Book> SearchBook(string title)
        {
            return _books
                .Where(b => b.Title.Contains(title, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        // Задача 16: Перегрузка методов SearchBook() (2 - поиск по автору)
        public List<Book> SearchBook(string author, bool byAuthor)
        {
            return _books
                .Where(b => b.Author.Contains(author, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        // Задача 16: Перегрузка методов SearchBook() (3 - поиск по году издания)
        public List<Book> SearchBook(int year)
        {
            return _books
                .Where(b => b.Year == year)
                .ToList();
        }

        // Задача 17: Перегрузка метода AddReader() (1 - только по имени)
        public void AddReader(string name)
        {
            SaveReaderToDb(name, null);
        }

        // Задача 17: Перегрузка метода AddReader() (2 - по имени и фамилии)
        public void AddReader(string firstName, string lastName)
        {
            string fullName = $"{firstName} {lastName}".Trim();
            SaveReaderToDb(fullName, null);
        }

        // Задача 17: Перегрузка метода AddReader() (3 - по имени и номеру телефона)
        public void AddReader(string name, string phone, bool withPhone)
        {
            SaveReaderToDb(name, phone);
        }

        private void SaveReaderToDb(string name, string? phone)
        {
            if (IsReaderRegistered(name))
            {
                Console.WriteLine($"Читатель \"{name}\" уже зарегистрирован.");
                return;
            }

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Readers (Name, Phone)
                VALUES ($name, $phone);
                SELECT last_insert_rowid();";
            cmd.Parameters.AddWithValue("$name", name);
            cmd.Parameters.AddWithValue("$phone", (object?)phone ?? DBNull.Value);

            int newId = Convert.ToInt32(cmd.ExecuteScalar());
            _readers.Add(new Reader(name, phone, newId));
            Console.WriteLine($"Читатель \"{name}\" успешно добавлен!");
        }

        // Задача 21: Метод выдачи книги BorrowBook(string title, string readerName)
        public void BorrowBook(string title, string readerName)
        {
            Book? book = FindBook(title);
            if (book == null)
            {
                Console.WriteLine($"Ошибка: Книга \"{title}\" не найдена в библиотеке.");
                return;
            }

            if (!book.IsBookAvailable())
            {
                Console.WriteLine($"Ошибка: Книга \"{title}\" уже выдана читателю {book.BorrowedBy}.");
                return;
            }

            if (!IsReaderRegistered(readerName))
            {
                Console.WriteLine($"Ошибка: Читатель \"{readerName}\" не зарегистрирован в системе.");
                return;
            }

            book.Available = false;
            book.BorrowedBy = readerName;

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var cmd = connection.CreateCommand();
            cmd.CommandText = "UPDATE Books SET Available = 0, BorrowedBy = $reader WHERE Id = $id";
            cmd.Parameters.AddWithValue("$reader", readerName);
            cmd.Parameters.AddWithValue("$id", book.Id);
            cmd.ExecuteNonQuery();

            Console.WriteLine($"Книга \"{book.Title}\" успешно выдана читателю {readerName}.");
        }

        // Задача 18: Метод с условием ReturnBook(string title)
        public void ReturnBook(string title)
        {
            ReturnBook(title, 0);
        }

        // Задача 22: Метод возврата книги ReturnBook() с проверкой статуса и расчётом штрафа за просрочку
        public void ReturnBook(string title, int overdueDays)
        {
            Book? book = FindBook(title);
            if (book == null)
            {
                Console.WriteLine($"Ошибка: Книга \"{title}\" не найдена в библиотеке.");
                return;
            }

            if (book.IsBookAvailable())
            {
                Console.WriteLine($"Книга \"{book.Title}\" уже находится в библиотеке (не была выдана).");
                return;
            }

            string previousHolder = book.BorrowedBy ?? "Неизвестный";
            book.Available = true;
            book.BorrowedBy = null;

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var cmd = connection.CreateCommand();
            cmd.CommandText = "UPDATE Books SET Available = 1, BorrowedBy = NULL WHERE Id = $id";
            cmd.Parameters.AddWithValue("$id", book.Id);
            cmd.ExecuteNonQuery();

            Console.WriteLine($"Книга \"{book.Title}\" успешно возвращена от читателя {previousHolder}.");

            if (overdueDays > 0)
            {
                decimal fine = CalculateFine(overdueDays);
                Console.WriteLine($"Внимание: Просрочка составила {overdueDays} дн. Штраф к оплате: {fine} тг.");
            }
            else
            {
                Console.WriteLine("Книга сдана вовремя. Штраф отсутствует.");
            }
        }

        // Задача 19: Рекурсивный метод CountBooksRecursive()
        public int CountBooksRecursive(int index = 0)
        {
            if (index >= _books.Count)
                return 0;
            return 1 + CountBooksRecursive(index + 1);
        }

        // Задача 20: Метод сортировки SortBooksByYear()
        public void SortBooksByYear()
        {
            _books.Sort((b1, b2) => b1.Year.CompareTo(b2.Year));
            Console.WriteLine("Книги отсортированы по году издания:");
            ShowBooks();
        }

        // Задача 23: Метод статистики ShowStatistics()
        public void ShowStatistics()
        {
            int total = GetBookCount();
            int available = GetAvailableBooks().Count;
            int borrowed = total - available;
            int readersCount = _readers.Count;

            Console.WriteLine("\n--- СТАТИСТИКА БИБЛИОТЕКИ ---");
            Console.WriteLine($"Общее количество книг: {total} (Счётчик TotalBooks: {TotalBooks}, Рекурсивный подсчёт: {CountBooksRecursive()})");
            Console.WriteLine($"Количество доступных книг: {available}");
            Console.WriteLine($"Количество выданных книг: {borrowed}");
            Console.WriteLine($"Количество зарегистрированных читателей: {readersCount}");
        }

        // Задача 24: Метод главного меню ShowMenu()
        public void ShowMenu()
        {
            Console.WriteLine("\n==============================");
            Console.WriteLine("     УПРАВЛЕНИЕ БИБЛИОТЕКОЙ");
            Console.WriteLine("==============================");
            Console.WriteLine("1. Добавить книгу");
            Console.WriteLine("2. Показать все книги");
            Console.WriteLine("3. Найти книгу");
            Console.WriteLine("4. Добавить читателя");
            Console.WriteLine("5. Показать читателей");
            Console.WriteLine("6. Выдать книгу");
            Console.WriteLine("7. Вернуть книгу");
            Console.WriteLine("8. Статистика библиотеки");
            Console.WriteLine("9. Дополнительные функции (Задачи 3, 5, 14, 20)");
            Console.WriteLine("0. Выход");
            Console.Write("Выберите действие: ");
        }
    }

    // ==========================================
    // ТОЧКА ВХОДА (Задача 25: Финальная интеграция)
    // ==========================================
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;

            // Задача 11: Вызов статического метода без создания объекта
            Library.ShowLibraryName();

            Library library = new Library();

            // Задача 2: Приветственное сообщение
            library.ShowWelcomeMessage();

            bool running = true;
            while (running)
            {
                library.ShowMenu();
                string? choice = Console.ReadLine()?.Trim();

                switch (choice)
                {
                    case "1":
                        Console.Write("Название: ");
                        string title = Console.ReadLine()?.Trim() ?? "Без названия";
                        Console.Write("Автор: ");
                        string author = Console.ReadLine()?.Trim() ?? "Неизвестен";
                        Console.Write("Год: ");
                        int.TryParse(Console.ReadLine(), out int year);
                        if (year == 0) year = DateTime.Now.Year;

                        library.AddBook(title, author, year);
                        break;

                    case "2":
                        library.ShowBooks();
                        break;

                    case "3":
                        Console.WriteLine("Варианты поиска (Задача 16): 1 - По названию | 2 - По автору | 3 - По году");
                        Console.Write("Ваш выбор: ");
                        string? searchType = Console.ReadLine()?.Trim();

                        List<Book> foundBooks = new List<Book>();
                        if (searchType == "1")
                        {
                            Console.Write("Введите название книги: ");
                            string qTitle = Console.ReadLine()?.Trim() ?? "";
                            foundBooks = library.SearchBook(qTitle);
                        }
                        else if (searchType == "2")
                        {
                            Console.Write("Введите автора: ");
                            string qAuthor = Console.ReadLine()?.Trim() ?? "";
                            foundBooks = library.SearchBook(qAuthor, true);
                        }
                        else if (searchType == "3")
                        {
                            Console.Write("Введите год издания: ");
                            int.TryParse(Console.ReadLine(), out int qYear);
                            foundBooks = library.SearchBook(qYear);
                        }

                        if (foundBooks.Count == 0)
                        {
                            Console.WriteLine("Книги по вашему запросу не найдены.");
                        }
                        else
                        {
                            Console.WriteLine($"Найдено книг: {foundBooks.Count}");
                            foreach (var b in foundBooks)
                                b.DisplayInfo();
                        }
                        break;

                    case "4":
                        Console.WriteLine("Способ добавления читателя (Задача 17): 1 - Только имя | 2 - Имя и фамилия | 3 - Имя и телефон");
                        Console.Write("Ваш выбор: ");
                        string? readerMode = Console.ReadLine()?.Trim();

                        if (readerMode == "2")
                        {
                            Console.Write("Введите имя: ");
                            string firstName = Console.ReadLine()?.Trim() ?? "";
                            Console.Write("Введите фамилию: ");
                            string lastName = Console.ReadLine()?.Trim() ?? "";
                            library.AddReader(firstName, lastName);
                        }
                        else if (readerMode == "3")
                        {
                            Console.Write("Введите имя: ");
                            string rName = Console.ReadLine()?.Trim() ?? "";
                            Console.Write("Введите номер телефона: ");
                            string phone = Console.ReadLine()?.Trim() ?? "";
                            library.AddReader(rName, phone, true);
                        }
                        else
                        {
                            Console.Write("Введите имя читателя: ");
                            string singleName = Console.ReadLine()?.Trim() ?? "";
                            library.AddReader(singleName);
                        }
                        break;

                    case "5":
                        library.ShowReaders();
                        break;

                    case "6":
                        Console.Write("Введите название книги для выдачи: ");
                        string borrowTitle = Console.ReadLine()?.Trim() ?? "";
                        Console.Write("Введите имя читателя: ");
                        string borrower = Console.ReadLine()?.Trim() ?? "";
                        library.BorrowBook(borrowTitle, borrower);
                        break;

                    case "7":
                        Console.Write("Введите название возвращаемой книги: ");
                        string returnTitle = Console.ReadLine()?.Trim() ?? "";
                        Console.Write("Количество дней просрочки (0 - если вовремя): ");
                        int.TryParse(Console.ReadLine(), out int overdueDays);

                        if (overdueDays > 0)
                            library.ReturnBook(returnTitle, overdueDays);
                        else
                            library.ReturnBook(returnTitle);
                        break;

                    case "8":
                        library.ShowStatistics();
                        break;

                    case "9":
                        RunAdditionalTasksMenu(library);
                        break;

                    case "0":
                        running = false;
                        Console.WriteLine("Работа программы завершена.");
                        break;

                    default:
                        Console.WriteLine("Неверный пункт меню. Попробуйте снова.");
                        break;
                }
            }
        }

        // Подменю для демонстрации изолированных задач (Задачи 3, 5, 14, 20)
        static void RunAdditionalTasksMenu(Library library)
        {
            Console.WriteLine("\n--- Демонстрация отдельных задач ---");
            Console.WriteLine("1. Задача 3: Изменить название книги (SetTitle)");
            Console.WriteLine("2. Задача 5: Создать 3 объекта Book и вызвать DisplayInfo()");
            Console.WriteLine("3. Задача 14: Сравнить количество книг (GetMaxBooks) и показать доступные");
            Console.WriteLine("4. Задача 20: Сортировать книги по году издания (SortBooksByYear)");
            Console.Write("Выберите задачу: ");
            string? sub = Console.ReadLine()?.Trim();

            switch (sub)
            {
                case "1":
                    Console.Write("Введите текущее название книги: ");
                    string oldT = Console.ReadLine()?.Trim() ?? "";
                    Console.Write("Введите новое название книги: ");
                    string newT = Console.ReadLine()?.Trim() ?? "";
                    if (library.RenameBook(oldT, newT))
                        Console.WriteLine("Название книги успешно обновлено!");
                    else
                        Console.WriteLine("Книга с таким названием не найдена.");
                    break;

                case "2":
                    // Задача 5: Создайте три объекта класса Book и вызовите DisplayInfo()
                    Book b1 = new Book("Чистый код", "Роберт Мартин", 2008);
                    Book b2 = new Book("Совершенный код", "Стив Макконнелл", 2004);
                    Book b3 = new Book("Паттерны проектирования", "Эрих Гамма", 1994);
                    b1.DisplayInfo();
                    b2.DisplayInfo();
                    b3.DisplayInfo();
                    break;

                case "3":
                    Console.Write("Введите количество книг в первом зале: ");
                    int.TryParse(Console.ReadLine(), out int firstCount);
                    Console.Write("Введите количество книг во втором зале: ");
                    int.TryParse(Console.ReadLine(), out int secondCount);
                    Console.WriteLine($"Большее количество книг (GetMaxBooks): {Library.GetMaxBooks(firstCount, secondCount)}");

                    var avail = library.GetAvailableBooks();
                    Console.WriteLine($"Сейчас доступно книг в библиотеке: {avail.Count}");
                    break;

                case "4":
                    library.SortBooksByYear();
                    break;
            }
        }
    }
}