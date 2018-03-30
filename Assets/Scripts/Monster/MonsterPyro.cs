﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MonsterPyro : MonsterBase
{

    public MonsterPyro(string name, string description, string spriteFile, int HP, int ATK, int DEF, int SPD)
    {
        this.name = name;
        this.description = description;
        this.spriteFile = spriteFile;
        this.baseHealth = HP;
        this.baseAttack = ATK;
        this.baseDefense = DEF;
        this.baseSpeed = SPD;
        this.baseType = BaseType.PYRO;

        //Attack test = new Attack(AttackType.SINGLE, "Wick Lash", "The enemy washes their wick at you", 3, 1f);
        //AddToMoveSet(test);

        AddToMoveSet(new Attack(AttackType.SINGLE, "Wick Lash", "The enemy washes their wick at you", 3, 1f));
        AddToMoveSet(new Attack(AttackType.SINGLE, "Inferno Flash", "The enemy quickly charges towards you!", 1, 1f));
        AddToMoveSet(new Attack(AttackType.SINGLE, "Fire Cannon", "An incoming wave of fire aims at your direction", 2, 1f));
    }
}