using System;
using System.Collections.Generic;
using BtlGame.Characters;
using BtlGame.Core;
using BtlGame.Properties;
using BtlGame.Skills;
using NUnit.Framework;

namespace BtlGame.Tests
{
    public sealed class RoleSkillRulesTests
    {
        [Test]
        public void CatalogContainsSixUniqueRolesAndCannotBeMutated()
        {
            var ids = new HashSet<RoleId>();
            foreach (RoleDefinition role in RoleCatalog.All)
            {
                Assert.That(ids.Add(role.Id), Is.True);
                Assert.That(role.Id, Is.Not.EqualTo(RoleId.None));
                Assert.That(RoleCatalog.Get(role.Id), Is.SameAs(role));
                Assert.That(role.DisplayName, Is.Not.Empty);
                Assert.That(role.PassiveDescription, Is.Not.Empty);
                Assert.That(role.ActiveDescription, Is.Not.Empty);
                Assert.That(role.UltimateDescription, Is.Not.Empty);
                Assert.That(role.WeaknessDescription, Is.Not.Empty);
            }
            Assert.That(ids.Count, Is.EqualTo(6));
            Assert.Throws<NotSupportedException>(() => ((IList<RoleDefinition>)RoleCatalog.All).Clear());
            Assert.Throws<ArgumentOutOfRangeException>(() => RoleCatalog.Get(RoleId.None));
        }

        [TestCase(RoleId.Investor, 5)]
        [TestCase(RoleId.Entrepreneur, 6)]
        [TestCase(RoleId.Engineer, 7)]
        [TestCase(RoleId.Lawyer, 6)]
        [TestCase(RoleId.Banker, 7)]
        [TestCase(RoleId.Strategist, 5)]
        public void ActiveCooldownMatchesApprovedRoleRules(RoleId role, int turns)
        {
            Assert.That(RoleCatalog.Get(role).ActiveCooldownTurns, Is.EqualTo(turns));
        }

        [TestCase(RoleId.Investor, false, 90000)]
        [TestCase(RoleId.Investor, true, 75000)]
        [TestCase(RoleId.Entrepreneur, false, 105000)]
        [TestCase(RoleId.Engineer, false, 100000)]
        [TestCase(RoleId.Lawyer, false, 100000)]
        [TestCase(RoleId.Banker, false, 100000)]
        [TestCase(RoleId.Strategist, false, 100000)]
        public void BankPurchaseAppliesDiscountOrWeaknessToListedPrice(RoleId role, bool coupon, long expected)
        {
            long listed = EconomyRules.ListedPrice(1, PropertyLevel.House3);
            long paid = EconomyRules.RentToPay(listed, SkillRules.BankPurchaseDiscountBasisPoints(role, coupon));
            Assert.That(paid, Is.EqualTo(expected));
        }

        [TestCase(RoleId.Investor, 22000)]
        [TestCase(RoleId.Engineer, 17000)]
        [TestCase(RoleId.Banker, 20000)]
        public void NormalUpgradeIncludesRoleSpecificCosts(RoleId role, long expected)
        {
            long difference = EconomyRules.ListedPrice(1, PropertyLevel.House3) -
                EconomyRules.ListedPrice(1, PropertyLevel.House2);
            Assert.That(EconomyRules.RentToPay(difference, SkillRules.NormalUpgradeDiscountBasisPoints(role)), Is.EqualTo(expected));
        }

        [TestCase(RoleId.Entrepreneur, 33000)]
        [TestCase(RoleId.Engineer, 27000)]
        [TestCase(RoleId.Strategist, 30000)]
        public void RoleRentModifierUsesTheBaseRent(RoleId role, long expected)
        {
            RentQuote quote = EconomyRules.CityRent(1, PropertyLevel.House3, SkillRules.RentModifierBasisPoints(role),
                false, false, false, false, false, false);
            Assert.That(quote.FinalRent, Is.EqualTo(expected));
        }

        [Test]
        public void EntrepreneurBonusesAreAddedBeforeDoublingAndAlsoApplyToBeach()
        {
            int modifier = SkillRules.RentModifierBasisPoints(RoleId.Entrepreneur) + 3000;
            Assert.That(EconomyRules.CityRent(1, PropertyLevel.House3, modifier,
                true, true, true, false, false, false).FinalRent, Is.EqualTo(126000));
            Assert.That(EconomyRules.BeachRent(2, modifier, true).FinalRent, Is.EqualTo(280000));
        }

        [Test]
        public void LawyerTaxCardReductionDoesNotChangeOtherRoles()
        {
            var properties = Array.Empty<PropertyState>();
            Assert.That(EconomyRules.TaxAssessment(300000, 1, properties,
                SkillRules.TaxCardReductionBasisPoints(RoleId.Lawyer)).FloorToLong(), Is.EqualTo(45000));
            Assert.That(EconomyRules.TaxAssessment(300000, 1, properties,
                SkillRules.TaxCardReductionBasisPoints(RoleId.Banker)).FloorToLong(), Is.EqualTo(60000));
        }

        [TestCase(PropertyLevel.Stake, 40000)]
        [TestCase(PropertyLevel.House1, 30000)]
        [TestCase(PropertyLevel.House2, 20000)]
        [TestCase(PropertyLevel.House3, 10000)]
        public void EngineerUltimatePaysHalfRemainingListedCostWithoutPassive(PropertyLevel current, long expected)
        {
            Assert.That(SkillRules.EngineerUltimateCost(1, current), Is.EqualTo(expected));
        }

        [Test]
        public void EngineerUltimateRejectsHotelAndBeachBeforeCalculatingCost()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => SkillRules.EngineerUltimateCost(1, PropertyLevel.Hotel));
            Assert.Throws<ArgumentException>(() => SkillRules.EngineerUltimateCost(4, PropertyLevel.Stake));
        }

        [Test]
        public void LegacyNoneRoleIsNeutralButUnknownRoleCannotSilentlyReceiveNeutralRules()
        {
            Assert.That(SkillRules.BankPurchaseDiscountBasisPoints(RoleId.None, false), Is.Zero);
            Assert.That(SkillRules.NormalUpgradeDiscountBasisPoints(RoleId.None), Is.Zero);
            Assert.That(SkillRules.RentModifierBasisPoints(RoleId.None), Is.Zero);
            Assert.That(SkillRules.TaxCardReductionBasisPoints(RoleId.None), Is.Zero);
            RoleId invalid = (RoleId)99;
            Assert.Throws<ArgumentOutOfRangeException>(() => SkillRules.BankPurchaseDiscountBasisPoints(invalid, false));
            Assert.Throws<ArgumentOutOfRangeException>(() => SkillRules.NormalUpgradeDiscountBasisPoints(invalid));
            Assert.Throws<ArgumentOutOfRangeException>(() => SkillRules.RentModifierBasisPoints(invalid));
            Assert.Throws<ArgumentOutOfRangeException>(() => SkillRules.TaxCardReductionBasisPoints(invalid));
        }
    }
}
