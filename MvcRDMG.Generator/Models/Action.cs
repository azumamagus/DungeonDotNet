namespace MvcRDMG.Generator.Models
{
    public class Action
    {
        public string Name { get; set; }
        public string Desc { get; set; }
        public int AttackBonus { get; set; }
        public string DamageDice { get; set; }
        public int? DamageBonus { get; set; }
    }
}