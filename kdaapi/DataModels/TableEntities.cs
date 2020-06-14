using Microsoft.Azure.Cosmos.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DevelopingInsanity.KDM.kdaapi.DataModels
{
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

    public class MonsterEntry : TableEntity
    {

        public MonsterEntry()
            : this(string.Empty, string.Empty)
        { 
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="monsterName">Name (eg. 'White Lion'). In case of version updates of the same monster, it can be listed with any number of '`' characters.</param>
        /// <param name="monsterLevel">Maps to MonsterLevel enum (eg. 'Prologue')</param>
        public MonsterEntry(string monsterName, string monsterLevel)
        {
            PartitionKey = monsterName;
            RowKey = monsterLevel;
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
    }
}
