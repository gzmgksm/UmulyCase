namespace UmulyCase.Models
{
    public class OfferViewModel
    {
        public int? Id { get; set; } 
        public string? Description { get; set; }
        public string? UserName { get; set; }
        public DateTime? OfferDate { get; set; }

        public virtual List<OfferDetailViewModel>? Details { get; set; }
        public OfferViewModel()
        {
            Id = 0;
            Description = String.Empty; ;
            UserName = String.Empty; ;
            OfferDate= DateTime.Today;
            Details = new List<OfferDetailViewModel>();

        }

    }
}
