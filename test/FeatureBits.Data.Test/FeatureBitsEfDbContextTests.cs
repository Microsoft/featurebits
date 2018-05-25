﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using FluentAssertions;
using Xunit;

namespace FeatureBits.Data.Test
{
    public class FeatureBitsEfDbContextTests : IDisposable
    {
        private readonly FeatureBitsEfDbContext _it;

        public FeatureBitsEfDbContextTests()
        {
            _it = FeatureBitEfHelper.SetupDbContext().Item1;
        }

        public void Dispose()
        {
            _it?.Dispose();
        }

        
        [Fact]
        public void ItCanBeCreated()
        {
            _it.Should().NotBeNull();
        }

        [Fact]
        public void ItHasAIFeatureBitDefinitionsDbSet()
        {
            _it.IFeatureBitDefinitions.Should().NotBeNull();
        }
    }
}
