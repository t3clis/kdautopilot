using Microsoft.Azure.Cosmos.Table;
using System;
using System.Text;

namespace DevelopingInsanity.KDM
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
        BasicAction,
        Instinct
    }

    public class MonsterCardEntity : TableEntity
    {
        public MonsterCardEntity()
        :this(string.Empty, string.Empty) { }

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

            sb.Append($"{(type == CardType.Instinct ? "Instinct:" : "")}{(type == CardType.BasicAction ? "Basic Action" : RowKey)} {(!string.IsNullOrEmpty(AILevel) ? "(" + AILevel + ")" : "")} [{Expansion} {Versions}]\n");


            switch (type)
            {
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
                case CardType.BasicAction:
                    {
                        sb.Append($"{CardText}\n");
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
