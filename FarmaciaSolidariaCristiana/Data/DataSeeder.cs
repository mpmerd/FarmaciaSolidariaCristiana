using FarmaciaSolidariaCristiana.Models;

namespace FarmaciaSolidariaCristiana.Data
{
    public static class DataSeeder
    {
        public static async Task SeedTestData(ApplicationDbContext context)
        {
            // Verificar si ya hay datos
            if (context.Medicines.Any())
            {
                return; // Ya hay datos, no hacer nada
            }

            // Crear medicamentos de prueba
            var medicines = new List<Medicine>
            {
                new Medicine
                {
                    Name = "Paracetamol 500mg",
                    Description = "Analgésico y antipirético para alivio del dolor y fiebre",
                    StockQuantity = 150,
                    Unit = "Comprimidos",
                    NationalCode = "653862"
                },
                new Medicine
                {
                    Name = "Ibuprofeno 400mg",
                    Description = "Antiinflamatorio no esteroideo para dolor e inflamación",
                    StockQuantity = 200,
                    Unit = "Comprimidos",
                    NationalCode = "659147"
                },
                new Medicine
                {
                    Name = "Amoxicilina 500mg",
                    Description = "Antibiótico de amplio espectro para infecciones bacterianas",
                    StockQuantity = 80,
                    Unit = "Cápsulas",
                    NationalCode = "750867"
                },
                new Medicine
                {
                    Name = "Omeprazol 20mg",
                    Description = "Inhibidor de la bomba de protones para protección gástrica",
                    StockQuantity = 120,
                    Unit = "Cápsulas",
                    NationalCode = "792952"
                },
                new Medicine
                {
                    Name = "Loratadina 10mg",
                    Description = "Antihistamínico para alergias y rinitis",
                    StockQuantity = 90,
                    Unit = "Comprimidos",
                    NationalCode = "663518"
                },
                new Medicine
                {
                    Name = "Metformina 850mg",
                    Description = "Antidiabético oral para control de glucosa en sangre",
                    StockQuantity = 100,
                    Unit = "Comprimidos",
                    NationalCode = "659080"
                },
                new Medicine
                {
                    Name = "Enalapril 10mg",
                    Description = "Antihipertensivo para control de presión arterial",
                    StockQuantity = 75,
                    Unit = "Comprimidos",
                    NationalCode = "659361"
                },
                new Medicine
                {
                    Name = "Atorvastatina 20mg",
                    Description = "Hipolipemiante para reducir colesterol",
                    StockQuantity = 60,
                    Unit = "Comprimidos",
                    NationalCode = "711127"
                },
                new Medicine
                {
                    Name = "Salbutamol Inhalador 100mcg",
                    Description = "Broncodilatador para asma y problemas respiratorios",
                    StockQuantity = 30,
                    Unit = "Inhaladores",
                    NationalCode = "659072"
                },
                new Medicine
                {
                    Name = "Insulina Glargina 100UI/ml",
                    Description = "Insulina de acción prolongada para diabetes",
                    StockQuantity = 15,
                    Unit = "Plumas",
                    NationalCode = "762930"
                },
                new Medicine
                {
                    Name = "Losartán 50mg",
                    Description = "Antihipertensivo antagonista de receptores de angiotensina II",
                    StockQuantity = 85,
                    Unit = "Comprimidos",
                    NationalCode = "714717"
                },
                new Medicine
                {
                    Name = "Ranitidina 150mg",
                    Description = "Antiácido para reducir producción de ácido estomacal",
                    StockQuantity = 50,
                    Unit = "Comprimidos",
                    NationalCode = "659049"
                }
            };

            context.Medicines.AddRange(medicines);
            await context.SaveChangesAsync();

            // Crear donaciones de prueba (últimos 3 meses)
            var donations = new List<Donation>
            {
                new Donation
                {
                    MedicineId = 1,
                    Quantity = 100,
                    DonationDate = DateTime.Now.AddMonths(-2).AddDays(-5),
                    DonorNote = "Farmacia El Salvador",
                    Comments = "Donación mensual regular"
                },
                new Donation
                {
                    MedicineId = 2,
                    Quantity = 150,
                    DonationDate = DateTime.Now.AddMonths(-2).AddDays(-3),
                    DonorNote = "Laboratorios Médicos Unidos",
                    Comments = "Excedente de inventario"
                },
                new Donation
                {
                    MedicineId = 3,
                    Quantity = 50,
                    DonationDate = DateTime.Now.AddMonths(-1).AddDays(-15),
                    DonorNote = "Hospital Regional",
                    Comments = "Medicamentos próximos a vencer pero en buen estado"
                },
                new Donation
                {
                    MedicineId = 4,
                    Quantity = 80,
                    DonationDate = DateTime.Now.AddMonths(-1).AddDays(-10),
                    DonorNote = "Farmacia San Juan",
                    Comments = "Donación de la comunidad"
                },
                new Donation
                {
                    MedicineId = 5,
                    Quantity = 60,
                    DonationDate = DateTime.Now.AddDays(-20),
                    DonorNote = "Donante Anónimo",
                    Comments = "Entregado en la iglesia"
                },
                new Donation
                {
                    MedicineId = 6,
                    Quantity = 70,
                    DonationDate = DateTime.Now.AddDays(-15),
                    DonorNote = "Asociación de Diabéticos",
                    Comments = "Campaña solidaria"
                },
                new Donation
                {
                    MedicineId = 9,
                    Quantity = 20,
                    DonationDate = DateTime.Now.AddDays(-8),
                    DonorNote = "Clínica Respiratoria",
                    Comments = "Apoyo a pacientes asmáticos"
                },
                new Donation
                {
                    MedicineId = 10,
                    Quantity = 10,
                    DonationDate = DateTime.Now.AddDays(-5),
                    DonorNote = "Fundación Diabetes",
                    Comments = "Donación especial para diabéticos"
                }
            };

            context.Donations.AddRange(donations);
            await context.SaveChangesAsync();

            // Crear entregas de prueba (últimos 2 meses)
            var deliveries = new List<Delivery>
            {
                new Delivery
                {
                    MedicineId = 1,
                    Quantity = 20,
                    DeliveryDate = DateTime.Now.AddMonths(-1).AddDays(-25),
                    PatientNote = "María González - DNI 12345678A",
                    Comments = "Para dolor de cabeza crónico"
                },
                new Delivery
                {
                    MedicineId = 2,
                    Quantity = 30,
                    DeliveryDate = DateTime.Now.AddMonths(-1).AddDays(-22),
                    PatientNote = "Juan Pérez - DNI 87654321B",
                    Comments = "Artritis reumatoide"
                },
                new Delivery
                {
                    MedicineId = 3,
                    Quantity = 15,
                    DeliveryDate = DateTime.Now.AddMonths(-1).AddDays(-18),
                    PatientNote = "Ana Martínez - DNI 11223344C",
                    Comments = "Infección respiratoria"
                },
                new Delivery
                {
                    MedicineId = 4,
                    Quantity = 25,
                    DeliveryDate = DateTime.Now.AddDays(-30),
                    PatientNote = "Carlos Rodríguez - DNI 55667788D",
                    Comments = "Gastritis crónica"
                },
                new Delivery
                {
                    MedicineId = 6,
                    Quantity = 15,
                    DeliveryDate = DateTime.Now.AddDays(-25),
                    PatientNote = "Isabel Torres - DNI 99887766E",
                    Comments = "Diabetes tipo 2"
                },
                new Delivery
                {
                    MedicineId = 7,
                    Quantity = 10,
                    DeliveryDate = DateTime.Now.AddDays(-20),
                    PatientNote = "Pedro Sánchez - DNI 44332211F",
                    Comments = "Hipertensión arterial"
                },
                new Delivery
                {
                    MedicineId = 1,
                    Quantity = 15,
                    DeliveryDate = DateTime.Now.AddDays(-15),
                    PatientNote = "Lucía Fernández - DNI 66778899G",
                    Comments = "Fiebre y malestar"
                },
                new Delivery
                {
                    MedicineId = 5,
                    Quantity = 10,
                    DeliveryDate = DateTime.Now.AddDays(-12),
                    PatientNote = "Miguel Ángel López - DNI 22334455H",
                    Comments = "Alergia estacional"
                },
                new Delivery
                {
                    MedicineId = 8,
                    Quantity = 15,
                    DeliveryDate = DateTime.Now.AddDays(-10),
                    PatientNote = "Carmen Ruiz - DNI 77889900I",
                    Comments = "Colesterol alto"
                },
                new Delivery
                {
                    MedicineId = 9,
                    Quantity = 5,
                    DeliveryDate = DateTime.Now.AddDays(-7),
                    PatientNote = "Javier Morales - DNI 33445566J",
                    Comments = "Crisis asmática"
                },
                new Delivery
                {
                    MedicineId = 10,
                    Quantity = 3,
                    DeliveryDate = DateTime.Now.AddDays(-5),
                    PatientNote = "Rosa García - DNI 88990011K",
                    Comments = "Diabetes tipo 1 - Urgente"
                },
                new Delivery
                {
                    MedicineId = 11,
                    Quantity = 10,
                    DeliveryDate = DateTime.Now.AddDays(-3),
                    PatientNote = "Antonio Jiménez - DNI 11223355L",
                    Comments = "Presión arterial elevada"
                },
                new Delivery
                {
                    MedicineId = 2,
                    Quantity = 20,
                    DeliveryDate = DateTime.Now.AddDays(-2),
                    PatientNote = "Elena Díaz - DNI 66554433M",
                    Comments = "Dolor muscular"
                },
                new Delivery
                {
                    MedicineId = 12,
                    Quantity = 8,
                    DeliveryDate = DateTime.Now.AddDays(-1),
                    PatientNote = "Francisco Vega - DNI 99887755N",
                    Comments = "Acidez estomacal"
                }
            };

            context.Deliveries.AddRange(deliveries);
            await context.SaveChangesAsync();
        }
    }
}
