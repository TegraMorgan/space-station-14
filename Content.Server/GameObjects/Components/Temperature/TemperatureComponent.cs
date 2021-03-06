﻿using Content.Server.Interfaces.GameObjects;
using Content.Shared.Maths;
using System;
using SS14.Shared.GameObjects;
using SS14.Shared.Utility;
using YamlDotNet.RepresentationModel;
using Content.Shared.GameObjects;
using SS14.Shared.Serialization;
using SS14.Shared.ViewVariables;

namespace Content.Server.GameObjects
{
    /// <summary>
    /// Handles changing temperature,
    /// informing others of the current temperature,
    /// and taking fire damage from high temperature.
    /// </summary>
    public class TemperatureComponent : Component, ITemperatureComponent
    {
        /// <inheritdoc />
        public override string Name => "Temperature";

        /// <inheritdoc />
        public override uint? NetID => ContentNetIDs.TEMPERATURE;

        //TODO: should be programmatic instead of how it currently is
        [ViewVariables]
        public float CurrentTemperature { get; private set; } = PhysicalConstants.ZERO_CELCIUS;

        float _fireDamageThreshold = 0;
        float _fireDamageCoefficient = 1;

        float _secondsSinceLastDamageUpdate = 0;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _fireDamageThreshold, "firedamagethreshold", 0);
            serializer.DataField(ref _fireDamageCoefficient, "firedamagecoefficient", 1);
        }

        /// <inheritdoc />
        public void OnUpdate(float frameTime)
        {
            int fireDamage = (int)Math.Floor(Math.Max(0, CurrentTemperature - _fireDamageThreshold) / _fireDamageCoefficient);

            _secondsSinceLastDamageUpdate += frameTime;

            Owner.TryGetComponent<DamageableComponent>(out DamageableComponent component);

            while (_secondsSinceLastDamageUpdate >= 1)
            {
                component?.TakeDamage(DamageType.Heat, fireDamage);
                _secondsSinceLastDamageUpdate -= 1;
            }
        }
    }
}
