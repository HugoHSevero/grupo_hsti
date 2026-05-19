using System;
using System.ComponentModel.DataAnnotations;

public class Chamado
{
    public int Id { get; set; }

    [Required(ErrorMessage = "O campo Título é obrigatório.")]
    [Display(Name = "Título")]
    public string Titulo { get; set; }

    [Required(ErrorMessage = "O campo Descrição é obrigatório.")]
    [Display(Name = "Descrição")]
    public string Descricao { get; set; }

    [Required(ErrorMessage = "Selecione uma Categoria.")]
    public CategoriaChamado Categoria { get; set; }

    [Required(ErrorMessage = "Selecione a Prioridade.")]

    public PrioridadeChamado Prioridade { get; set; }

    public StatusChamado Status { get; set; }

    public DateTime DataCriacao { get; set; }

    public string UsuarioId { get; set; }

    public ApplicationUser Usuario { get; set; }

    public List<MensagemChamado> Mensagens { get; set; }
}