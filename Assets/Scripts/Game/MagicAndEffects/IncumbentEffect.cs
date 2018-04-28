﻿// Project:         Daggerfall Tools For Unity
// Copyright:       Copyright (C) 2009-2018 Daggerfall Workshop
// Web Site:        http://www.dfworkshop.net
// License:         MIT License (http://www.opensource.org/licenses/mit-license.php)
// Source Code:     https://github.com/Interkarma/daggerfall-unity
// Original Author: Gavin Clayton (interkarma@dfworkshop.net)
// Contributors:    
// 
// Notes:
//

using UnityEngine;
using DaggerfallWorkshop.Game.Entity;

namespace DaggerfallWorkshop.Game.MagicAndEffects.MagicEffects
{
    /// <summary>
    /// Some effects in Daggerfall add to the state an existing like-kind effect (the incumbent)
    /// rather than become intantiated as a new effect on the host entity.
    /// One example is a drain effect which only adds to the magnitude of incumbent drain for same stat.
    /// Another example is an effect which tops up the duration of same effect in progress.
    /// This classes establishes a base for these incumbent effects to coordinate.
    /// </summary>
    public abstract class IncumbentEffect : BaseEntityEffect
    {
        bool isIncumbent = false;

        public override void Start(EntityEffectManager manager, DaggerfallEntityBehaviour caster = null)
        {
            base.Start(manager, caster);
            AttachHost();
        }

        protected bool IsIncumbent
        {
            get { return isIncumbent; }
        }

        void AttachHost()
        {
            IncumbentEffect incumbent = FindIncumbent();
            if (incumbent == null)
            {
                // First instance of effect on this host becomes incumbent
                isIncumbent = true;
                BecomeIncumbent();

                //Debug.LogFormat("Creating incumbent effect '{0}' on host '{1}'", DisplayName, manager.name);
            }
            else
            {
                // Subsequent instances add to state of incumbent
                incumbent.AddState(this);

                //Debug.LogFormat("Adding state to incumbent effect '{0}' on host '{1}'", incumbent.DisplayName, incumbent.manager.name);
            }
        }

        IncumbentEffect FindIncumbent()
        {
            // Search for any incumbents on this host
            EntityEffectManager.InstancedBundle[] bundles = manager.EffectBundles;
            foreach (EntityEffectManager.InstancedBundle bundle in bundles)
            {
                foreach (IEntityEffect effect in bundle.effects)
                {
                    if (effect is IncumbentEffect)
                    {
                        // Effect must be flagged incumbent and agree with like-kind test
                        IncumbentEffect other = effect as IncumbentEffect;
                        if (other.IsIncumbent && other.IsLikeKind(this))
                            return other;
                    }
                }
            }

            return null;
        }

        protected abstract bool IsLikeKind(IncumbentEffect other);
        protected abstract void BecomeIncumbent();
        protected abstract void AddState(IncumbentEffect incumbent);
    }
}