﻿using NUnit.Framework;
using Umbraco.Core.Persistence.Mappers;
using Umbraco.Core.Persistence.SqlSyntax;

namespace Umbraco.Tests.Persistence.Mappers
{
    [TestFixture]
    public class PropertyGroupMapperTest
    {
        [Test]
        public void Can_Map_Id_Property()
        {
            // Act
            string column = new PropertyGroupMapper(new SqlCeSyntaxProvider()).Map("Id");

            // Assert
            Assert.That(column, Is.EqualTo("[cmsPropertyTypeGroup].[id]"));
        }

        [Test]
        public void Can_Map_ParentId_Property()
        {
            // Act
            string column = new PropertyGroupMapper(new SqlCeSyntaxProvider()).Map("ParentId");

            // Assert
            Assert.That(column, Is.EqualTo("[cmsPropertyTypeGroup].[parentGroupId]"));
        }

        [Test]
        public void Can_Map_SortOrder_Property()
        {
            // Act
            string column = new PropertyGroupMapper(new SqlCeSyntaxProvider()).Map("SortOrder");

            // Assert
            Assert.That(column, Is.EqualTo("[cmsPropertyTypeGroup].[sortorder]"));
        }

        [Test]
        public void Can_Map_Name_Property()
        {

            // Act
            string column = new PropertyGroupMapper(new SqlCeSyntaxProvider()).Map("Name");

            // Assert
            Assert.That(column, Is.EqualTo("[cmsPropertyTypeGroup].[text]"));
        }
    }
}