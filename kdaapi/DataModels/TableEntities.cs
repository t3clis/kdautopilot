using Microsoft.Azure.Cosmos.Table;
using System;
using System.Linq;
using Newtonsoft.Json;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DevelopingInsanity.KDM.kdaapi.DataModels
{
    public enum CardType
    {
        Unknown,
        AI,
        HitLocation,
        BasicResource,
        MonsterResource,
        StrangeResource,
        HuntEvent,
        Universal
    }

    public enum MonsterLevel
    {
        Undefined,
        Prologue,
        L1,
        L2,
        L3,
        L4,
        Legendary
    }

    public enum ProgressionStage
    {
        HuntPhase,
        ShowdownPhaseSetup,
        Showdown,
        ShowdownAftermath,
        ShowdownComplete
    }

    public class DataUtils
    {
        public static bool CompareStringLists(string firstList, string secondList)
        {
            return CompareStringLists(firstList, secondList, ",", true);
        }

        public static bool CompareStringLists(string firstList, string secondList, string separator, bool ignoreCase)
        {
            string[] src = firstList.Split(separator);
            string[] cmp = secondList.Split(separator);
            StringComparison options = ignoreCase ? StringComparison.InvariantCultureIgnoreCase : StringComparison.InvariantCulture;

            foreach (string s in src)
                foreach (string v in cmp)
                    if (s.Trim().Equals(v.Trim(), options))
                        return true;

            return false;
        }

        public static int GetVersionNumber(string version)
        {
            string[] versionItems = version.Split(".");
            Array.Reverse(versionItems);
            int result = 0;
            for (int i = 0; i < versionItems.Length; i++)
            {
                result += (int.Parse(versionItems[i]) * (int)Math.Pow(100, i));
            }

            return result;
        }

        public static int PickHighestVersionNumberInList(string versionList)
        {
            int highest = -1;
            string[] versions = versionList.Split(",");
            
            foreach (string v in versions)
            {
                int num = GetVersionNumber(v.Trim());
                if (num > highest)
                    highest = num;
            }

            return highest;
        }

        public static bool HasVersionNumberInList(string numberToSearch, string versionList)
        {
            string[] versions = versionList.Split(",");

            foreach (string v in versions)
            {
                int num = GetVersionNumber(numberToSearch);
                int ver = GetVersionNumber(v.Trim());

                if (ver == num)
                    return true;
            }

            return false;
        }
    }

    public class MonsterInstanceEntity : TableEntity
    {
        public const string TABLE_NAME = "Instances";

        //partitionId = GUID of the monster created
        //rowId = progressive (zero-based) number indicating the progression in the showdown


        public string MonsterName { get; set; }
        public string Level { get; set; }
        public string Version { get; set; }
        public string AIDeck { get; set; }
        public string AIDiscard { get; set; }
        public string HLDeck { get; set; }
        public string HLDiscard { get; set; }
        public string ResourceDeck { get; set; }
        public string HuntEventSteps { get; set; }
        public string HuntingPartyPosition { get; set; }
        public string MonsterPosition { get; set; }
        public string LifePoints { get; set; }
        public string Progression { get; set; }
        public string MonsterStates { get; set; }
        public string AdditionalAttributes { get; set; }

        public MonsterInstanceEntity()
            : this(Guid.NewGuid().ToString(), "0")
        {
        }

        public MonsterInstanceEntity(string instanceId, string pointInTime)
        {
            PartitionKey = instanceId;
            RowKey = pointInTime;
            MonsterName = string.Empty;
            Level = string.Empty;
            Version = string.Empty;
            AIDeck = string.Empty;
            AIDiscard = string.Empty;
            HLDeck = string.Empty;
            HLDiscard = string.Empty;
            ResourceDeck = string.Empty;
            HuntEventSteps = string.Empty;
            HuntingPartyPosition = string.Empty;
            MonsterPosition = string.Empty;
            LifePoints = string.Empty;
            Progression = string.Empty;
            MonsterStates = string.Empty;
            AdditionalAttributes = string.Empty;
        }

        public static MonsterInstanceEntity Generate(CloudTableClient client, string monsterName, string level, string version)
        {
            MonsterInstanceEntity result = new MonsterInstanceEntity();
            MonsterEntity baseMonster = null;

            CloudTable monsterTable = client.GetTableReference(MonsterEntity.TABLE_NAME);
            var candidates = monsterTable.ExecuteQuery(new TableQuery<MonsterEntity>()).Where(p => { return p.Name.StartsWith(monsterName, StringComparison.InvariantCultureIgnoreCase); }).ToList();

            if (string.IsNullOrEmpty(version))
            {
                int highestVersion = -1;

                //pick latest version
                foreach (MonsterEntity candidate in candidates)
                {
                    try
                    {
                        int ver = DataUtils.PickHighestVersionNumberInList(candidate.Version);
                        if (ver > highestVersion)
                        {
                            highestVersion = ver;
                            baseMonster = candidate;
                        }
                    }
                    catch (FormatException)
                    {
                        
                    }
                }
            }
            else
            {
                //pick exact version
                try
                {
                    int ver = DataUtils.GetVersionNumber(version);
                    foreach (MonsterEntity candidate in candidates)
                    {
                        //TODO: Pick exact version
                    }
                }
                catch (FormatException)
                { }
            }

            if (baseMonster == null)
                return null;

            return result;
        }

        public async Task<bool> Save(CloudTable table)
        {
            TableOperation op = TableOperation.Retrieve<MonsterInstanceEntity>(PartitionKey, RowKey);

            TableResult check = await table.ExecuteAsync(op);

            if ((check.Result as MonsterInstanceEntity) != null)
                return false;

            //TODO: proper insertion

            return true;
        }
    }

    public class MonsterEntity : TableEntity
    {
        public const string TABLE_NAME = "Monsters";

        public MonsterEntity()
            : this(string.Empty, string.Empty)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="monsterName">Name (eg. 'White Lion'). In case of version updates of the same monster, it can be listed with any number of '`' characters.</param>
        /// <param name="monsterLevel">Maps to MonsterLevel enum (eg. 'Prologue')</param>
        public MonsterEntity(string monsterName, string monsterLevel)
        {
            PartitionKey = monsterName;
            RowKey = monsterLevel;
            Name = string.Empty;
            Expansion = string.Empty;
            Version = string.Empty;
            Cards = string.Empty;
            Traits = string.Empty;
            CardsInPlay = string.Empty;
            Movement = string.Empty;
            Toughness = string.Empty;
            Speed = string.Empty;
            Damage = string.Empty;
            Tokens = string.Empty;
            HuntTableSteps = string.Empty;
            MonsterPositionInHuntTable = string.Empty;
            AdditionalModifiers = string.Empty;
            Instinct = string.Empty;
            BasicAction = string.Empty;
        }

        /// <summary>
        /// Monster name. It is usually the same as PartitionKey, but it eschews ` and has sometimes alternate values
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Expansion name (eg. 'Core')
        /// </summary>
        public string Expansion { get; set; }
        /// <summary>
        /// Expansion version (eg. '1.5')
        /// </summary>
        public string Version { get; set; }
        /// <summary>
        /// Comma-separated list of cards (eg. 'Chomp,Claw' and so on).
        /// A card level listed in curly brackets (eg. '{B}') means a random card from the deck of that specific level.
        /// A number in square brackets preceding card name (eg. '[0]Claw') indicates its absolute position (0-index) in the deck, after shuffling)
        /// A ~ preceding card name (eg. '~Vanish') means that the card in the deck is face up.
        /// </summary>
        public string Cards { get; set; }
        /// <summary>
        /// Comma-separated list of traits (eg. 'Cunning')
        /// </summary>
        public string Traits { get; set; }
        /// <summary>
        /// Comma-separated list of cards already in play. Follows rules from card list, except that cards are already in play
        /// </summary>
        public string CardsInPlay { get; set; }
        /// <summary>
        /// Monster movement. It can be a string representation of a number (eg. '3'), or a special value in curly brackets (eg. {Infinite}).
        /// </summary>
        public string Movement { get; set; }
        /// <summary>
        /// Monster toughness. It can be a string representation of a number (eg. '3'), or a special value in curly brackets (eg. {CurrentLanternYear})
        /// </summary>
        public string Toughness { get; set; }
        /// <summary>
        ///  Monster speed modifier. It can be a string representation of a number (eg. '3'), or a special value in curly brackets (eg. {CurrentLanternYear})
        /// </summary>
        public string Speed { get; set; }
        /// <summary>
        /// Monster damage modifier. It can be a string representation of a number (eg. '3'), or a special value in curly brackets (eg. {CurrentLanternYear})
        /// </summary>
        public string Damage { get; set; }
        /// <summary>
        /// Comma-separated list of tokens in play, each token in round brackets (eg. '(+dmg),(-spd)')
        /// </summary>
        public string Tokens { get; set; }
        /// <summary>
        /// Comma-separated list of hunt steps, in curly brackets (eg. '{Basic},{Monster},{Basic},{OverwhelmingDarkness}'). Multiple events for the same step are separated by a pipe '|' character
        /// </summary>
        public string HuntTableSteps { get; set; }
        /// <summary>
        /// String representing integer of monster position in hunt table steps.
        /// </summary>
        public string MonsterPositionInHuntTable { get; set; }
        /// <summary>
        /// Comma-separated list of additional effects applying to the monster, each in curly brackets and with its own set of parameter (eg. '{Ambushed}')
        /// </summary>
        public string AdditionalModifiers { get; set; }
        /// <summary>
        /// Monster instinct, written with the same annotation syntax of a monster card
        /// </summary>
        public string Instinct { get; set; }
        /// <summary>
        /// Monster Basic Action, written with the same annotation syntax of a monster
        /// </summary>
        public string BasicAction { get; set; }

        public string Serialize()
        {
            return JsonConvert.SerializeObject(this);
        }

        public static MonsterEntity Deserialize(string json)
        {
            return JsonConvert.DeserializeObject<MonsterEntity>(json);
        }
    }

    public class MonsterCardEntity : TableEntity
    {
        public MonsterCardEntity()
        : this(string.Empty, string.Empty) { }

        public MonsterCardEntity(string cardType, string cardTitle)
        {
            PartitionKey = cardType;
            RowKey = cardTitle;
            Multiplicity = 1;
            ResourceKeywords = string.Empty;
            CardText = string.Empty;
            Expansion = string.Empty;
            Versions = string.Empty;
            AILevel = string.Empty;
            AITypes = string.Empty;
            EventSubtitle = string.Empty;
            EventBottomText = string.Empty;
            HLCriticalText = string.Empty;
            HLTypes = string.Empty;
            HLReactionFailure = string.Empty;
            HLReactionReflex = string.Empty;
            HLReactionWound = string.Empty;
            BasicAction = string.Empty;
            Instinct = string.Empty;
        }

        public string ResourceKeywords { get; set; }
        public string CardText { get; set; }
        public string Expansion { get; set; }
        public string Versions { get; set; }
        public string AILevel { get; set; }
        public string AITypes { get; set; }
        public string EventSubtitle { get; set; }
        public string EventBottomText { get; set; }
        public string HLCriticalText { get; set; }
        public string HLTypes { get; set; }
        public string HLReactionWound { get; set; }
        public string HLReactionFailure { get; set; }
        public string HLReactionReflex { get; set; }
        public string BasicAction { get; set; }
        public string Instinct { get; set; }
        public int Multiplicity { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            CardType type;

            try
            {
                type = Enum.Parse<CardType>(PartitionKey);
            }
            catch (Exception)
            {
                type = CardType.Unknown;
            }

            sb.Append($"{RowKey} {(!string.IsNullOrEmpty(AILevel) ? "(" + AILevel + ")" : "")} [{Expansion} {Versions}]\n");


            switch (type)
            {
                case CardType.Universal:
                case CardType.AI:
                    {
                        sb.Append($"{AITypes}\n{CardText}\n");
                    }
                    break;
                case CardType.HitLocation:
                    {
                        sb.Append($"{HLTypes}\n{CardText}\n{HLReactionFailure}\n{HLReactionWound}\n{HLReactionReflex}\n{HLCriticalText}\n");
                    }
                    break;
                case CardType.HuntEvent:
                    {
                        sb.Append($"{EventSubtitle}\n{CardText}\n{EventBottomText}\n");
                    }
                    break;
                case CardType.MonsterResource:
                case CardType.BasicResource:
                case CardType.StrangeResource:
                    {
                        sb.Append($"{ResourceKeywords}\n{CardText}\n");
                    }
                    break;
                case CardType.Unknown:
                default:
                    {
                        sb.Append($"{CardText}\n");
                    }
                    break;
            }

            return sb.ToString();
        }
    }

    public class IndexByMonsterEntity : TableEntity
    {
        public IndexByMonsterEntity() : this(string.Empty, string.Empty) { }

        public IndexByMonsterEntity(string monster, string cardTitle)
        {
            PartitionKey = monster;
            RowKey = cardTitle;
            CardType = string.Empty;
        }

        public string CardType { get; set; }

        public override string ToString()
        {
            return $"{PartitionKey} :: {RowKey} [{CardType}]";
        }
    }
}
