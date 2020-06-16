using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
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
        Find,
        RebuildIndexes,
        DumpTables,
        RebuildTables
    }

    internal class ProgramParameters
    {
        public OperationRequested Operation { get; set; }
        public CardType CardType { get; set; }
        public string CardName { get; set; }
        public string DataFile { get; set; }

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
        private const string cardsTableName = "MonsterCards", indexTableName = "IndexByMonsterNames", monsterTableName = "Monsters";
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
            bool expectType = false, expectName = false, expectFile = false;
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
                            if (result.Operation != OperationRequested.Unknown)
                                result.Operation = OperationRequested.Invalid;
                            else
                                result.Operation = OperationRequested.List;
                        }
                        break;
                    case "-del":
                        {
                            if (result.Operation != OperationRequested.Unknown)
                                result.Operation = OperationRequested.Invalid;
                            else
                            {
                                result.Operation = OperationRequested.Delete;
                                expectName = true;
                            }
                        }
                        break;
                    case "-find":
                        {
                            if (result.Operation != OperationRequested.Unknown)
                                result.Operation = OperationRequested.Invalid;
                            else
                            {
                                result.Operation = OperationRequested.Find;
                                expectName = true;
                            }
                        }
                        break;
                    case "--rebuild-indexes":
                        {
                            if (result.Operation != OperationRequested.Unknown)
                                result.Operation = OperationRequested.Invalid;
                            else
                                result.Operation = OperationRequested.RebuildIndexes;
                        }
                        break;
                    case "--dump":
                        {
                            if (result.Operation != OperationRequested.Unknown)
                                result.Operation = OperationRequested.Invalid;
                            else
                            {
                                result.Operation = OperationRequested.DumpTables;
                                expectFile = true;
                            }
                        }
                        break;
                    case "--upload":
                        {
                            if (result.Operation != OperationRequested.Unknown)
                                result.Operation = OperationRequested.Invalid;
                            else
                            {
                                result.Operation = OperationRequested.RebuildTables;
                                expectFile = true;
                            }
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
                        else if (expectName)
                        {
                            result.CardName = arg;
                            expectName = false;
                        }
                        else if (expectFile)
                        {
                            result.DataFile = arg;
                            expectFile = false;
                        }
                        else
                            result.Operation = OperationRequested.Invalid;
                        break;
                }
            }

            if (result.Operation == OperationRequested.Unknown || expectType || expectName || expectFile)
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

        static string ReadOption(string prompt, params string[] options)
        {
            bool inputValid = false;
            string input = string.Empty;

            while (!inputValid)
            {
                input = ReadNotEmptyLine(prompt);
                inputValid = options.Contains(input);
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

        static void AddContinuation()
        {
            Console.Write("Press ENTER to continue...");
            Console.CursorVisible = false;
            Console.ReadLine();
            Console.CursorVisible = true;
        }

        static void Main(string[] args)
        {

            AppSettings settings = null;
            CloudStorageAccount account = null;
            CloudTable cardsTable = null, indexTable = null, monsterTable = null;
            ProgramParameters parameters = ParseParameters(args, out bool verbose);


            if (parameters.Operation == OperationRequested.Invalid)
            {
                Console.WriteLine("Usage:\n\tkdacli (-add {type} | -del \"card name\" | -l | -find {\"card name\"|cardType} | --rebuild-indexes | --dump {file}| --upload {file})[-v | --verbose]");
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
            monsterTable = Task.Run(async () => await Common.CreateTableAsync(account, monsterTableName)).GetAwaiter().GetResult();

            switch (parameters.Operation)
            {
                case OperationRequested.Add:
                    {
                        ExecuteAdd(parameters, cardsTable, indexTable);
                    }
                    break;
                case OperationRequested.List:
                    {
                        ExecuteList(cardsTable, indexTable);
                    }
                    break;
                case OperationRequested.Find:
                    {
                        ExecuteFind(parameters, cardsTable);
                    }
                    break;
                case OperationRequested.Delete:
                    {
                        ExecuteDelete(parameters, cardsTable, indexTable);
                    }
                    break;
                case OperationRequested.RebuildIndexes:
                    {
                        ExecuteRebuildIndexes(cardsTable, indexTable);
                    }
                    break;
                case OperationRequested.DumpTables:
                    {
                        List<CloudTable> tables = new List<CloudTable>();
                        List<string> names = new List<string>();

                        ExecuteDump(cardsTable, indexTable, monsterTable, parameters.DataFile);
                    }
                    break;
                case OperationRequested.RebuildTables:
                    {
                        ExecuteUpload(account, parameters.DataFile);
                    }
                    break;
            }
        }

        private static void ExecuteUpload(CloudStorageAccount account, string sourceDataFile)
        {
            DumpSerialization dump = null;

            try
            {
                Console.Write("Loading source data file...");
                dump = DumpSerialization.FromFile(sourceDataFile);
                Console.WriteLine("done");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"failed\nError accessing source data file '{sourceDataFile}':\n{ex}");
                return;
            }

            //todo: same considerations as the ExecuteDump() todo annotation

            Console.Write($"Inserting entries for table '{cardsTableName}'...");
            CloudTable t = Task.Run(async () => await Common.CreateTableAsync(account, dump.Cards.TableName)).GetAwaiter().GetResult();
            foreach (MonsterCardEntity e in dump.Cards.Items)
            {
                TableOperation op = TableOperation.InsertOrReplace(e);
                t.ExecuteAsync(op).GetAwaiter().GetResult();
            }
            Console.WriteLine($"done - {dump.Cards.Items.Length} entries uploaded");

            Console.Write($"Inserting entries for table '{indexTableName}'...");
            t = Task.Run(async () => await Common.CreateTableAsync(account, dump.Index.TableName)).GetAwaiter().GetResult();
            foreach (IndexByMonsterEntity e in dump.Index.Items)
            {
                TableOperation op = TableOperation.InsertOrReplace(e);
                t.ExecuteAsync(op).GetAwaiter().GetResult();
            }
            Console.WriteLine($"done - {dump.Index.Items.Length} entries uploaded");

            Console.Write($"Inserting entries for table '{monsterTableName}'...");
            t = Task.Run(async () => await Common.CreateTableAsync(account, dump.Monsters.TableName)).GetAwaiter().GetResult();
            foreach (MonsterEntry e in dump.Monsters.Items)
            {
                TableOperation op = TableOperation.InsertOrReplace(e);
                t.ExecuteAsync(op).GetAwaiter().GetResult();
            }
            Console.WriteLine($"done - {dump.Monsters.Items.Length} entries uploaded");
        }

        private static void ExecuteDump(CloudTable cards, CloudTable index, CloudTable monsters, string targetDataFile)
        {
            DumpSerialization dump = new DumpSerialization();

            //todo: we will swap this with a good powershell cmdlet, eventually.
            //at the moment, we are okay with a static count of items - we just need to populate the initial list

            Console.WriteLine($"Dump of table '{cardsTableName}'");
            dump.Cards = new CardsTableDescriptor
            {
                TableName = cardsTableName
            };
            Console.Write("Retrieving rows...");
            var cardsEntities = cards.ExecuteQuery(new TableQuery<MonsterCardEntity>()).ToList();
            dump.Cards.Items = cardsEntities.ToArray();
            Console.WriteLine($"done - {dump.Cards.Items.Length} entries retrieved");

            Console.WriteLine($"Dump of table '{indexTableName}'");
            dump.Index = new IndexTableDescriptor
            {
                TableName = indexTableName
            };
            Console.Write("Retrieving rows...");
            var indexEntities = index.ExecuteQuery(new TableQuery<IndexByMonsterEntity>()).ToList();
            dump.Index.Items = indexEntities.ToArray();
            Console.WriteLine($"done - {dump.Index.Items.Length} entries retrieved");

            Console.WriteLine($"Dump of table '{monsterTableName}'");
            dump.Monsters = new MonstersTableDescriptor
            {
                TableName = monsterTableName
            };
            Console.Write("Retrieving rows...");
            var monsterEntities = monsters.ExecuteQuery(new TableQuery<MonsterEntry>()).ToList();
            dump.Monsters.Items = monsterEntities.ToArray();
            Console.WriteLine($"done - {dump.Monsters.Items.Length} entries retrieved");

            Console.Write("Saving dump to file...");
            dump.Save(targetDataFile);
            Console.WriteLine("done\n");
        }

        private static void ExecuteRebuildIndexes(CloudTable cardsTable, CloudTable indexTable)
        {
            TableUtils<IndexByMonsterEntity> indexUtils = new TableUtils<IndexByMonsterEntity>(indexTable);
            int count = 0;
            List<IndexByMonsterEntity> rebuiltEntries = new List<IndexByMonsterEntity>();

            Console.WriteLine("Rebuild indexes");

            Console.Write("Index by Monster: retrieving current index...");
            var indexEntities = indexTable.ExecuteQuery(new TableQuery<IndexByMonsterEntity>()).ToList();
            Console.WriteLine("done");

            Console.WriteLine("Index by Monster: rebuilding properties");
            var cardEntities = cardsTable.ExecuteQuery(new TableQuery<MonsterCardEntity>()).ToList();

            foreach (MonsterCardEntity entity in cardEntities)
            {
                if (entity.PartitionKey != CardType.BasicResource.ToString()
                    && entity.PartitionKey != CardType.StrangeResource.ToString())
                {
                    foreach (IndexByMonsterEntity indexEntry in indexEntities)
                    {
                        if (entity.RowKey.Equals(indexEntry.RowKey) && !indexEntry.CardType.Equals(entity.PartitionKey))
                        {
                            indexEntry.CardType = entity.PartitionKey;
                            rebuiltEntries.Add(indexEntry);
                            Console.WriteLine($"Entry '{indexEntry}' updated");
                            count++;
                        }
                    }
                }
            }

            if (count > 0)
            {
                Console.Write("Index by monster: uploading rebuilt index...");
                indexUtils.BatchInsertOrMergeEntityAsync(indexEntities, true).GetAwaiter().GetResult();
                Console.WriteLine($"done\n{indexEntities.Count} entr{(indexEntities.Count == 1 ? "y" : "ies")} found\n{count} entr{(count == 1 ? "y" : "ies")} rebuilt");
            }
            else
            {
                Console.WriteLine("Index by Monster: no entries to update found.");
            }
        }

        private static void ExecuteDelete(ProgramParameters parameters, CloudTable cardsTable, CloudTable indexTable)
        {
            TableUtils<MonsterCardEntity> cardUtils = new TableUtils<MonsterCardEntity>(cardsTable);
            TableUtils<IndexByMonsterEntity> indexUtils = new TableUtils<IndexByMonsterEntity>(indexTable);

            Console.Write("Retrieving entities to be deleted...");
            var cards = cardsTable.ExecuteQuery<MonsterCardEntity>(new TableQuery<MonsterCardEntity>()).Where(e =>
            { return e.RowKey.StartsWith(parameters.CardName); }).ToList();
            Console.WriteLine($"done\n{cards.Count} entries found.");

            foreach (MonsterCardEntity card in cards)
            {
                string option;
                Console.WriteLine($"**************\n{card}\n**************\nIf you confirm deletion, this card and all its index entries will be deleted.");
                option = ReadOption("Are you sure (y/n)? ", "Y", "y", "yes", "Yes", "N", "n", "No", "no");
                switch (option)
                {
                    case "y":
                    case "Y":
                    case "yes":
                    case "Yes":
                        {
                            Console.Write("Searching related index entries...");
                            var indexEntries = indexTable.ExecuteQuery(new TableQuery<IndexByMonsterEntity>()).Where((e) => { return e.RowKey.Equals(card.RowKey); }).ToList();
                            Console.WriteLine($"done\n{indexEntries.Count} entr{(indexEntries.Count == 1 ? "y" : "ies")} found.");
                            Console.Write("Deleting card entry...");
                            cardUtils.DeleteEntityAsync(card).GetAwaiter().GetResult();
                            Console.WriteLine("done");

                            Console.Write("Deleting index entries...");
                            foreach (IndexByMonsterEntity indexEntry in indexEntries)
                            {
                                indexUtils.DeleteEntityAsync(indexEntry).GetAwaiter().GetResult();
                            }
                            Console.WriteLine("done");
                        }
                        break;
                    default:
                        {
                            Console.Write("Skipping this entry. ");
                        }
                        break;
                }

                AddContinuation();
            }
        }

        private static void ExecuteFind(ProgramParameters parameters, CloudTable cardsTable)
        {
            Console.Write("Querying cards table...");
            var entities = cardsTable.ExecuteQuery(new TableQuery<MonsterCardEntity>()).Where((item) =>
                                                {
                                                    return
                                                      item.PartitionKey.Contains(parameters.CardName, StringComparison.InvariantCultureIgnoreCase)
                                                    || item.RowKey.Contains(parameters.CardName, StringComparison.InvariantCultureIgnoreCase);
                                                }).ToList();
            Console.WriteLine($"done\n{entities.Count} entr{(entities.Count != 1 ? "ies" : "y")} found.\n\n*******************");

            foreach (MonsterCardEntity entity in entities)
            {
                Console.WriteLine($"{entity}\n*******************");
                AddContinuation();
            }
        }

        private static void ExecuteList(CloudTable cardsTable, CloudTable indexTable)
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

            AddContinuation();

            Console.WriteLine("Card entries:\n*******************");

            count = 0;

            foreach (MonsterCardEntity card in cardEntities)
            {
                count++;
                Console.WriteLine($"{card}\n*******************");
                if (count % 3 == 0)
                {
                    AddContinuation();
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
            monsterCard.RowKey = ReadNotEmptyLine("Card name: ");

            MonsterCardEntity existingCard = cardUtils.RetrieveEntityUsingPointQueryAsync(monsterCard.PartitionKey, monsterCard.RowKey).GetAwaiter().GetResult();

            if (existingCard != null)
            {
                Console.WriteLine($"Entry '{monsterCard.PartitionKey} {monsterCard.RowKey}' already exists in database. If you continue, existing entry will be REPLACED.");
                string option = ReadOption("Continue (yes/no)? ", "Y", "y", "yes", "Yes", "N", "n", "No", "no");
                switch (option)
                {
                    case "N":
                    case "n":
                    case "no":
                    case "No":
                        {
                            Console.WriteLine("Aborted by user.");
                            return;
                        }
                }
            }

            indexCard.RowKey = monsterCard.RowKey;

            monsterCard.Expansion = ReadNotEmptyLine("Expansion: ");
            monsterCard.Versions = ReadNotEmptyLine("Versions: ");

            if (parameters.CardType != CardType.StrangeResource && parameters.CardType != CardType.BasicResource && parameters.CardType != CardType.Universal)
            {
                indexCard.PartitionKey = ReadNotEmptyLine("Monster: ");
            }

            switch (parameters.CardType)
            {
                case CardType.AI:
                case CardType.Universal:
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

            if (parameters.CardType != CardType.BasicResource && parameters.CardType != CardType.StrangeResource && parameters.CardType != CardType.Universal)
            {
                indexUtils.InsertOrMergeEntityAsync(indexCard, true).GetAwaiter().GetResult();
            }

            Console.WriteLine("Data added.");
        }
    }
}
