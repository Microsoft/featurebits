﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Data;
using System.Threading.Tasks;
using Dotnet.FBit.CommandOptions;
using FeatureBits.Core;
using FeatureBits.Data;

namespace Dotnet.FBit.Command
{
    /// <summary>
    /// Class that represents the command to add (or update) a feature bit
    /// </summary>
    public class AddCommand
    {
        private readonly AddOptions _opts;
        private readonly IFeatureBitsRepo _repo;

        public AddCommand(AddOptions opts, IFeatureBitsRepo repo)
        {
            _opts = opts ?? throw new ArgumentNullException(nameof(opts), "AddOptions object is required.");
            _repo = repo ?? throw new ArgumentNullException(nameof(repo), "FeatureBits repository is required.");
        }

        public async Task<int> RunAsync()
        {
            int returnValue = 0;
            try
            {
                IFeatureBitDefinition newBit = BuildBit();
                await _repo.AddAsync(newBit);
                SystemContext.ConsoleWriteLine("Feature bit added.");
            }
            catch (DataException e)
            {
                returnValue = await HandleFeatureBitAlreadyExists(e);
            }
            catch (Exception e)
            {
                returnValue = 1;
                SystemContext.ConsoleErrorWriteLine(e.ToString());
            }

            return returnValue;
        }

        public IFeatureBitDefinition BuildBit()
        {
            var now = SystemContext.Now();
            var username = SystemContext.GetEnvironmentVariable("USERNAME");
            var onOffFlag = ParseOnOff();
            return new CommandFeatureBitDefintion
            {
                Name = _opts.Name,
                CreatedDateTime = now,
                LastModifiedDateTime = now,
                CreatedByUser = username,
                LastModifiedByUser = username,
                OnOff = onOffFlag,
                ExcludedEnvironments = _opts.ExcludedEnvironments,
                MinimumAllowedPermissionLevel = _opts.MinimumPermissionLevel,
                ExactAllowedPermissionLevel = _opts.ExactPermissionLevel,
                IncludedEnvironments = _opts.IncludedEnvironments
            };
        }

        private bool ParseOnOff()
        {
            bool success = bool.TryParse(_opts.OnOff, out var onOffFlag);
            if (!success)
                onOffFlag = false;
            return onOffFlag;
        }

        private async Task<int> HandleFeatureBitAlreadyExists(DataException e)
        {
            int returnValue;
            if (e.Message == ($"Cannot add. Feature bit with name '{_opts.Name}' already exists."))
            {
                returnValue = !_opts.Force ? FailWithoutForce() : await ForceUpdate();
            }
            else
            {
                returnValue = 1;
                SystemContext.ConsoleErrorWriteLine(e.ToString());
            }

            return returnValue;
        }

        private async Task<int> ForceUpdate()
        {
            var newBit = BuildBit();
            await _repo.UpdateAsync(newBit);
            SystemContext.ConsoleWriteLine("Feature bit updated.");
            return 0;
        }

        private int FailWithoutForce()
        {
            SystemContext.ConsoleErrorWriteLine(
                $"Feature bit '{_opts.Name}' already exists. Use --force to overwrite existing feature bits.");
            return 1;
        }
    }
}
