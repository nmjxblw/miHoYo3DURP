using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
[CreateAssetMenu(fileName = "CharacterConfigSO", menuName = "Character Config")]
public class CharacterConfigSO : ScriptableObject
{
    public int maxHp;
    public int attack;
    public float movingSpeed;
    public List<Skill> skills;
}
[Serializable]
public class Skill : IComparable<Skill>
{
    public string name;
    public float coolDown;
    public int CompareTo(Skill other)
    {
        return this.name.CompareTo(other.name);
    }
}