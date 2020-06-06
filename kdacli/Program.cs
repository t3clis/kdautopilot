using Microsoft.Azure.Cosmos.Table;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace DevelopingInsanity.KDM.kdacli
{
    internal enum OperationRequested
    {
        Unknown,
        Invalid,
        List,
        Add,
        Delete,
        Find
    }

    internal class ProgramParameters
    {
        public OperationRequested Operation { get; set; }
        public CardType CardType { get; set; }
        public string CardName { get; set; }

        public ProgramParameters()
        : this(OperationRequested.Unknown)
        { }

        public ProgramParameters(OperationRequested operation)
        {
            Operation = operation;
        }

    }


    class Program
    {
        static void Out(string text, bool verbose)
        {
            if (verbose)
                Console.Write(text);
        }

        static void OutL(string text, bool verbose)
        {
            if (verbose)
                Console.WriteLine(text);
        }

        static ProgramParameters ParseParameters(string[] args, out bool verbose)
        {
            ProgramParameters result = new ProgramParameters();
            bool expectType = false;
            verbose = false;

            foreach (string arg in args)
            {
                Trace.TraceInformation($"Parsing argument {arg}, operation: {result}");

                if (result.Operation == OperationRequested.Invalid)
                    break;

                switch (arg)
                {
                    case "-v":
                    case "/v":
                    case "--verbose":
                        {
                            verbose = true;
                        }
                        break;
                    case "-add":
                        {
                            if (result.Operation != OperationRequested.Unknown)
                                result.Operation = OperationRequested.Invalid;
                            else
                            {
                                result.Operation = OperationRequested.Add;
                                expectType = true;
                            }
                        }
                        break;
                    case "-l":
                        {
                            if (expectType)
                                result.Operation = OperationRequested.Invalid;
                            else if (result.Operation != OperationRequested.Unknown)
                                result.Operation = OperationRequested.Invalid;
                            else
                                result.Operation = OperationRequested.List;
                        }
                        break;
                    default:
                        if (expectType)
                        {
                            if (Enum.TryParse(arg, out CardType type))
                            {
                                result.CardType = type;
                                expectType = false;
                            }
                            else
                            {
                                result.Operation = OperationRequested.Invalid;
                                expectType = false;
                            }
                        }
                        else
                            result.Operation = OperationRequested.Invalid;
                        break;
                }
            }

            if (result.Operation == OperationRequested.Unknown || expectType)
                result.Operation = OperationRequested.Invalid;

            return result;
        }

        static string ReadNotEmptyLine(string prompt)
        {
            bool inputValid = false;
            string input = string.Empty;

            while (!inputValid)
            {
                Console.Write(prompt);
                input = Console.ReadLine().Trim();
                inputValid = (!string.IsNullOrEmpty(input) && !string.IsNullOrWhiteSpace(input));
            }

            return input;
        }

        static int ReadInt(string prompt, int? defaultValue)
        {
            bool inputValid = false;
            int value = defaultValue.HasValue ? defaultValue.Value : 0;
            string input;

            while (!inputValid)
            {
                Console.Write(prompt);
                input = Console.ReadLine();

                if (string.IsNullOrEmpty(input) && defaultValue.HasValue)
                    inputValid = true;
                else
                    inputValid = int.TryParse(input, out value);
            }

            return value;
        }

        static void Main(string[] args)
        {
            string cardsTableName = "MonsterCards", indexTableName = "IndexByMonsterNames";
            bool verbose = false;
            AppSettings settings = null;
            CloudStorageAccount account = null;
            CloudTable cardsTable = null, indexTable = null;
            ProgramParameters parameters = ParseParameters(args, out verbose);


            if (parameters.Operation == OperationRequested.Invalid)
            {
                Console.WriteLine("Usage:\n\tkdacli (-add {type} | -del {card_name} | -l | -find {card_name|card_type})[-v | --verbose]");
                Environment.Exit(-1);
            }

            try
            {
                Out("Loading application settings...", verbose);
                settings = AppSettings.LoadAppSettings();
                OutL($"done\nSAS Token in use: {settings.SASToken}\nStorage account: {settings.StorageAccountName}", verbose);
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Error loading application settings:\n{ex}");
                OutL($"error\nFailed to load application settings ({ex.Message})", verbose);
                Environment.Exit(-1);
            }

            try
            {
                Out("Logging in to storage account...", verbose);
                account = Common.CreateStorageAccountFromSASToken(settings.SASToken, settings.StorageAccountName);
                OutL("done\n", verbose);
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Error logging in to storage account:\n{ex}");
                OutL($"error\nLogin failed ({ex.Message})", verbose);
                Environment.Exit(-1);
            }


            cardsTable = Task.Run(async () => await Common.CreateTableAsync(account, cardsTableName)).GetAwaiter().GetResult();
            indexTable = Task.Run(async () => await Common.CreateTableAsync(account, indexTableName)).GetAwaiter().GetResult();

            switch (parameters.Operation)
            {
                case OperationRequested.Add:
                    {
                        ExecuteAdd(parameters, cardsTable, indexTable);
                    }
                    break;
                case OperationRequested.List:
                    {
                        ExecuteList(parameters, cardsTable, indexTable);
                    }
                    break;
            }
        }

        private static void ExecuteList(ProgramParameters parameters, CloudTable cardsTable, CloudTable indexTable)
        {
            int count;

            Console.Write("Querying index-by-monster table...");
            var indexEntities = indexTable.ExecuteQuery(new TableQuery<IndexByMonsterEntity>()).ToList();
            Console.WriteLine("done");

            Console.Write("Querying cards table...");
            var cardEntities = cardsTable.ExecuteQuery(new TableQuery<MonsterCardEntity>()).ToList();
            Console.WriteLine("done");

            Console.WriteLine("Card names indexed by monster:\n*******************");

            foreach (IndexByMonsterEntity ie in indexEntities)
            {
                Console.WriteLine($"{ie}");
            }

            Console.WriteLine("*******************");

            Console.WriteLine("Card entries:\n*******************");

            count = 0;

            foreach (MonsterCardEntity card in cardEntities)
            {
                count++;
                Console.WriteLine($"{card}\n*******************");
                if (count % 3 == 0)
                {
                    Console.Write("Press ENTER to continue...");
                    Console.ReadLine();
                }
            }
        }

        private static void ExecuteAdd(ProgramParameters parameters, CloudTable cardsTable, CloudTable indexTable)
        {
            MonsterCardEntity monsterCard = new MonsterCardEntity();
            IndexByMonsterEntity indexCard = new IndexByMonsterEntity();
            TableUtils<MonsterCardEntity> cardUtils = new TableUtils<MonsterCardEntity>(cardsTable);
            TableUtils<IndexByMonsterEntity> indexUtils = new TableUtils<IndexByMonsterEntity>(indexTable);


            monsterCard.PartitionKey = parameters.CardType.ToString();
            indexCard.CardType = parameters.CardType.ToString();

            if (parameters.CardType != CardType.BasicAction)
            {
                monsterCard.RowKey = ReadNotEmptyLine("Card name: ");
            }
            else
            {
                monsterCard.RowKey = "Basic Action";
            }

            indexCard.RowKey = monsterCard.RowKey;

            monsterCard.Expansion = ReadNotEmptyLine("Expansion: ");
            monsterCard.Versions = ReadNotEmptyLine("Versions: ");

            if (parameters.CardType != CardType.StrangeResource && parameters.CardType != CardType.BasicResource)
            {
                indexCard.PartitionKey = ReadNotEmptyLine("Monster: ");
            }

            switch (parameters.CardType)
            {
                case CardType.AI:
                    {
                        monsterCard.AILevel = ReadNotEmptyLine("AI Level: ");
                        Console.Write("AI card types (if any): ");
                        monsterCard.AITypes = Console.ReadLine();
                        monsterCard.CardText = ReadNotEmptyLine("Card Text: ");
                    }
                    break;
                case CardType.MonsterResource:
                case CardType.BasicResource:
                case CardType.StrangeResource:
                    {
                        monsterCard.ResourceKeywords = ReadNotEmptyLine("Keywords: ");
                        monsterCard.CardText = ReadNotEmptyLine("Card Text: ");
                    }
                    break;
                case CardType.BasicAction:
                case CardType.Instinct:
                    {
                        monsterCard.CardText = ReadNotEmptyLine("Card Text: ");
                    }
                    break;
                case CardType.HitLocation:
                    {
                        Console.Write("HL types (if any): ");
                        monsterCard.HLTypes = Console.ReadLine();
                        Console.Write("Card text (if any): ");
                        monsterCard.CardText = Console.ReadLine();
                        Console.Write("Reaction - Failure (if any): ");
                        monsterCard.HLReactionFailure = Console.ReadLine();
                        Console.Write("Reaction - Wound (if any): ");
                        monsterCard.HLReactionWound = Console.ReadLine();
                        Console.Write("Reaction - Reflex (if any): ");
                        monsterCard.HLReactionReflex = Console.ReadLine();
                        Console.Write("Critical Wound text (if any): ");
                        monsterCard.HLCriticalText = Console.ReadLine();
                    }
                    break;
                case CardType.HuntEvent:
                    {
                        monsterCard.EventSubtitle = ReadNotEmptyLine("Event subtitle: ");
                        monsterCard.CardText = ReadNotEmptyLine("Card text: ");
                        Console.Write("Event bottom text (if any): ");
                        monsterCard.EventBottomText = Console.ReadLine();
                    }
                    break;
                default:
                    {
                        Trace.TraceError($"Unable to parse CardType '{parameters.CardType}'");
                        return;
                    }
            }

            monsterCard.Multiplicity = ReadInt("# of cards (default 1): ", 1);

            Trace.TraceInformation($"Adding to database: [{monsterCard}]");
            Console.WriteLine($"*************\n{monsterCard}\n*************");

            cardUtils.InsertOrMergeEntityAsync(monsterCard, true).GetAwaiter().GetResult();

            if (parameters.CardType != CardType.BasicResource && parameters.CardType != CardType.StrangeResource)
            {
                indexUtils.InsertOrMergeEntityAsync(indexCard, true).GetAwaiter().GetResult();
            }

            Console.WriteLine("Data added.");
        }
    }
}
