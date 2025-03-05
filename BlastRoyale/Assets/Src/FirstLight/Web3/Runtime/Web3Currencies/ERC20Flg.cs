using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Sequence.ABI;
using Sequence.Provider;
using UnityEngine;
using System.Numerics;
using static Sequence.Contracts.Contract;

namespace Sequence.Contracts.Flg
{
    public class ERC20Flg : Ownable
    {
        public new Contract Contract { get; private set; }
        public static readonly new string Abi = "[{\"inputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"constructor\"},{\"anonymous\":false,\"inputs\":[{\"indexed\":true,\"internalType\":\"address\",\"name\":\"owner\",\"type\":\"address\"},{\"indexed\":true,\"internalType\":\"address\",\"name\":\"spender\",\"type\":\"address\"},{\"indexed\":false,\"internalType\":\"uint256\",\"name\":\"value\",\"type\":\"uint256\"}],\"name\":\"Approval\",\"type\":\"event\"},{\"anonymous\":false,\"inputs\":[{\"indexed\":true,\"internalType\":\"address\",\"name\":\"previousOwner\",\"type\":\"address\"},{\"indexed\":true,\"internalType\":\"address\",\"name\":\"newOwner\",\"type\":\"address\"}],\"name\":\"OwnershipTransferred\",\"type\":\"event\"},{\"anonymous\":false,\"inputs\":[{\"indexed\":true,\"internalType\":\"address\",\"name\":\"from\",\"type\":\"address\"},{\"indexed\":true,\"internalType\":\"address\",\"name\":\"to\",\"type\":\"address\"},{\"indexed\":false,\"internalType\":\"uint256\",\"name\":\"value\",\"type\":\"uint256\"}],\"name\":\"Transfer\",\"type\":\"event\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"owner\",\"type\":\"address\"},{\"internalType\":\"address\",\"name\":\"spender\",\"type\":\"address\"}],\"name\":\"allowance\",\"outputs\":[{\"internalType\":\"uint256\",\"name\":\"\",\"type\":\"uint256\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"spender\",\"type\":\"address\"},{\"internalType\":\"uint256\",\"name\":\"amount\",\"type\":\"uint256\"}],\"name\":\"approve\",\"outputs\":[{\"internalType\":\"bool\",\"name\":\"\",\"type\":\"bool\"}],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"account\",\"type\":\"address\"}],\"name\":\"balanceOf\",\"outputs\":[{\"internalType\":\"uint256\",\"name\":\"\",\"type\":\"uint256\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"uint256\",\"name\":\"amount\",\"type\":\"uint256\"}],\"name\":\"burn\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"account\",\"type\":\"address\"},{\"internalType\":\"uint256\",\"name\":\"amount\",\"type\":\"uint256\"}],\"name\":\"burnFrom\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[],\"name\":\"decimals\",\"outputs\":[{\"internalType\":\"uint8\",\"name\":\"\",\"type\":\"uint8\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"spender\",\"type\":\"address\"},{\"internalType\":\"uint256\",\"name\":\"subtractedValue\",\"type\":\"uint256\"}],\"name\":\"decreaseAllowance\",\"outputs\":[{\"internalType\":\"bool\",\"name\":\"\",\"type\":\"bool\"}],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"spender\",\"type\":\"address\"},{\"internalType\":\"uint256\",\"name\":\"addedValue\",\"type\":\"uint256\"}],\"name\":\"increaseAllowance\",\"outputs\":[{\"internalType\":\"bool\",\"name\":\"\",\"type\":\"bool\"}],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"to\",\"type\":\"address\"},{\"internalType\":\"uint256\",\"name\":\"amount\",\"type\":\"uint256\"}],\"name\":\"mint\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[],\"name\":\"name\",\"outputs\":[{\"internalType\":\"string\",\"name\":\"\",\"type\":\"string\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[],\"name\":\"owner\",\"outputs\":[{\"internalType\":\"address\",\"name\":\"\",\"type\":\"address\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[],\"name\":\"renounceOwnership\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[],\"name\":\"symbol\",\"outputs\":[{\"internalType\":\"string\",\"name\":\"\",\"type\":\"string\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[],\"name\":\"totalSupply\",\"outputs\":[{\"internalType\":\"uint256\",\"name\":\"\",\"type\":\"uint256\"}],\"stateMutability\":\"view\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"to\",\"type\":\"address\"},{\"internalType\":\"uint256\",\"name\":\"amount\",\"type\":\"uint256\"}],\"name\":\"transfer\",\"outputs\":[{\"internalType\":\"bool\",\"name\":\"\",\"type\":\"bool\"}],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"from\",\"type\":\"address\"},{\"internalType\":\"address\",\"name\":\"to\",\"type\":\"address\"},{\"internalType\":\"uint256\",\"name\":\"amount\",\"type\":\"uint256\"}],\"name\":\"transferFrom\",\"outputs\":[{\"internalType\":\"bool\",\"name\":\"\",\"type\":\"bool\"}],\"stateMutability\":\"nonpayable\",\"type\":\"function\"},{\"inputs\":[{\"internalType\":\"address\",\"name\":\"newOwner\",\"type\":\"address\"}],\"name\":\"transferOwnership\",\"outputs\":[],\"stateMutability\":\"nonpayable\",\"type\":\"function\"}]";
        
        public ERC20Flg(Contract contract) : base(contract)
        {
            this.Contract = contract;
        }

        /// <summary>
        /// Create an ERC20 contract
        /// Use the default ABI if none is provided
        /// </summary>
        /// <param name="contractAddress"></param>
        /// <param name="abi"></param>
        public ERC20Flg(string contractAddress, string abi = null) : base (contractAddress)
        {
            if (abi == null)
            {
                this.Contract = new Contract(contractAddress, Abi);
            }
            else
            {
                this.Contract = new Contract(contractAddress, abi);
            }
        }

        public Address GetAddress()
        {
            return Contract.GetAddress();
        }

        public async Task<string> Name(IEthClient client)
        {
            string result = await Contract.SendQuery<string>(client, "name");
            return result;
        }

        public async Task<string> Symbol(IEthClient client)
        {
            string result = await Contract.SendQuery<string>(client, "symbol");
            return result;
        }

        public async Task<BigInteger> Decimals(IEthClient client)
        {
            BigInteger result = await Contract.SendQuery<BigInteger>(client, "decimals");
            return result;
        }

        public async Task<BigInteger> TotalSupply(IEthClient client)
        {
            BigInteger result = await Contract.SendQuery<BigInteger>(client, "totalSupply");
            return result;
        }

        public async Task<BigInteger> BalanceOf(IEthClient client, string address)
        {
            BigInteger result = await Contract.SendQuery<BigInteger>(client, "balanceOf", address);
            return result;
        }

        public async Task<BigInteger> Allowance(IEthClient client, string ownerAddress, string spenderAddress)
        {
            BigInteger result = await Contract.SendQuery<BigInteger>(client, "allowance", ownerAddress, spenderAddress);
            return result;
        }

        public CallContractFunction Transfer(string toAddress, BigInteger amount)
        {
            return Contract.CallFunction("transfer", new Address(toAddress), amount);
        }

        public CallContractFunction Approve(string spenderAddress, BigInteger amount)
        {
            return Contract.CallFunction("approve", spenderAddress, amount);
        }

        public CallContractFunction TransferFrom(string fromAddress, string toAddress, BigInteger amount)
        {
            return Contract.CallFunction("transferFrom", fromAddress, toAddress, amount);
        }
        

        #region mintable
        public CallContractFunction Mint(string toAddress, BigInteger amount)
        {
            return Contract.CallFunction("mint", toAddress, amount);
        }
        #endregion

        #region burnable
        public CallContractFunction Burn(BigInteger amount)
        {
            return Contract.CallFunction("burn", amount);
        }

        public CallContractFunction BurnFrom(string fromAddress, BigInteger amount)
        {
            return Contract.CallFunction("burnFrom", fromAddress, amount);
        }
        #endregion
    }
}
