using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using APIService.Data;
using APIService.Models;
using APIService.Services;
using System.Linq;
using System;

namespace APIService.Controllers
{
    [ApiController]
    [Route("")]
    public class ContractController : ControllerBase
    {
        private DataContext _context;
        private IPrestacaoService _service;

        public ContractController(
            DataContext context)
        {
            _context = context;
            _service = new PrestacaoService();
        }

        [HttpGet]
        [Route("contrato")]
        public async Task<List<Contrato>> GetContracts()
        {
            var contracts = await _context.Contratos
                .Include(x => x.Prestacoes)
                .ToListAsync();
            foreach (Contrato contract in contracts)
            {
                contract.SetPrestacoes(await GetPrestacoesByContrato(contract.Id));
                _context.Contratos.Update(contract);
            }
            return contracts;
        }

        [HttpGet]
        [Route("contrato/{id:int}")]
        public async Task<Contrato> GetContractById(int id)
        {
            var contrato = await _context.Contratos
                .Include(x => x.Prestacoes)
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);
            return contrato;
        }

        [HttpGet]
        [Route("prestacao/{idContrato:int}")]
        public async Task<List<Prestacao>> GetPrestacoesByContrato(int idContrato)
        {
            var prestacoes = await _context.Prestacoes
                .AsNoTracking()
                .Where(x => x.IdContrato == idContrato)
                .ToListAsync();
            return prestacoes;
        }

        [HttpGet]
        [Route("prestacao")]
        public async Task<List<Prestacao>> GetPrestacoes()
        {
            var prestacoes = await _context.Prestacoes
                .ToListAsync();
            return prestacoes;
        }

        [HttpPost]
        [Route("contrato")]
        public async Task<ActionResult<Contrato>> PostContract([FromBody] Contrato model)
        {
            if (ModelState.IsValid)
            {
                model.SetDataContratacao(DateTime.Now);
                _context.Contratos.Add(model);
                await _context.SaveChangesAsync();

                List<Prestacao> prestacoes = await _service.GerarPrestacoes(model);
                foreach (Prestacao prestacao in prestacoes)
                {
                    _context.Prestacoes.Add(prestacao);
                }

                model.SetPrestacoes(prestacoes);
                _context.Contratos.Update(model);
                await _context.SaveChangesAsync();
                return model;
            }
            else
            {
                return BadRequest(ModelState);         
            }
        }

        [HttpPut]
        [Route("prestacao/{id:int}")]
        public async Task<IActionResult> AtualizarPrestacao(int id, [FromBody] Prestacao prestacao)
        {
            if (prestacao == null || prestacao.DataVencimento == null)
                return BadRequest();
            
            var _prestacao = await _context.Prestacoes.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);

            if (_prestacao == null)
                return NotFound();
            
            _prestacao.DataVencimento = prestacao.DataVencimento;
            _prestacao.DataPagamento = prestacao.DataPagamento;
            _prestacao.Status = await CheckStatus(_prestacao.DataPagamento, _prestacao.DataVencimento);

            _context.Prestacoes.Update(_prestacao);
            await _context.SaveChangesAsync();

            return new NoContentResult();
        }

        [NonAction]
        public async Task<string> CheckStatus(string dataPagamento, DateTime dataVencimento)
        {
            await Task.Delay(0);

            if (!string.IsNullOrEmpty(dataPagamento))
            {
                return "Baixada";
            }
            else if (dataVencimento.CompareTo(DateTime.Now) < 0)
            {
                return "Atrasada";
            }
            else return "Aberta";
        }

    }
}