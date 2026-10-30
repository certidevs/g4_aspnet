using System;

namespace G4AspNet.Models;

public class Category
{
<<<<<<< HEAD
    public int Id{get; set;}
    public string Name {get;set;} = string.Empty;
    public string Description{get; set;} = string.Empty;

    public Ticket? Ticket {get; set;}
=======
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Ticket? Ticket { get; set; }
    public int TicketId { set; get; }
>>>>>>> af2711292ea4fe2a8319142f4316891fd5955f94

    public int TicketId {get; set;}

    public ICollection<Ticket> Tickets { get; set; }
    = new List<Ticket>();
}
