namespace Jobsy.EvaluationManagement.Domain.Model.ValueObjects;

public class ScenarioOption
{
    public string id { get; set; }
    public string text { get; set; }
    public int score { get; set; }
    public string feedback { get; set; }
}

public class EvaluationScenario
{
    public string id { get; set; }
    public string skill { get; set; }
    public string skill_label { get; set; }
    public string situation { get; set; }
    public List<ScenarioOption> options { get; set; }
}

public static class EvaluationScenarios
{
    public static readonly List<EvaluationScenario> All = new()
    {
        new EvaluationScenario
        {
            id = "esc-1", skill = "comunicacion", skill_label = "Comunicación",
            situation = "Un compañero de equipo no entiende una instrucción que le diste sobre una tarea urgente. ¿Qué haces?",
            options = new List<ScenarioOption>
            {
                new() { id = "esc-1-a", text = "Le repites exactamente lo mismo, más despacio.", score = 30, feedback = "Repetir lo mismo rara vez mejora la comprensión — mejor buscar otro enfoque." },
                new() { id = "esc-1-b", text = "Le preguntas qué parte no quedó clara y se lo explicas con un ejemplo.", score = 100, feedback = "¡Excelente! Escuchar activamente y usar ejemplos concretos mejora mucho la comunicación." },
                new() { id = "esc-1-c", text = "Le dices que lo revise por su cuenta, no tienes tiempo.", score = 10, feedback = "Esto puede generar errores costosos y dañar la confianza del equipo." }
            }
        },
        new EvaluationScenario
        {
            id = "esc-2", skill = "trabajo_equipo", skill_label = "Trabajo en equipo",
            situation = "Tu equipo está dividido entre dos enfoques para resolver un problema y el plazo se acerca. ¿Qué haces?",
            options = new List<ScenarioOption>
            {
                new() { id = "esc-2-a", text = "Impones tu propia idea porque estás seguro de que es la mejor.", score = 30, feedback = "Imponer una idea sin escuchar puede generar resentimiento en el equipo." },
                new() { id = "esc-2-b", text = "Propones evaluar rápido los pros y contras de ambas juntos y decidir en equipo.", score = 100, feedback = "¡Muy bien! Una decisión colaborativa suele generar más compromiso del equipo." },
                new() { id = "esc-2-c", text = "Evitas el tema y dejas que el líder decida solo.", score = 40, feedback = "Evitar la conversación puede hacer perder ideas valiosas del equipo." }
            }
        },
        new EvaluationScenario
        {
            id = "esc-3", skill = "resolucion_problemas", skill_label = "Resolución de problemas",
            situation = "Descubres un error crítico en un entregable justo antes de la fecha de entrega. ¿Qué haces?",
            options = new List<ScenarioOption>
            {
                new() { id = "esc-3-a", text = "Entregas igual y esperas que nadie lo note.", score = 5, feedback = "Ocultar un error crítico suele generar consecuencias mucho peores después." },
                new() { id = "esc-3-b", text = "Avisas de inmediato al equipo/responsable y propones un plan para corregirlo.", score = 100, feedback = "¡Perfecto! La transparencia y la acción rápida son clave para resolver problemas." },
                new() { id = "esc-3-c", text = "Intentas arreglarlo solo sin avisar a nadie, aunque tome más tiempo.", score = 50, feedback = "Buena intención, pero no avisar retrasa el apoyo que el equipo podría darte." }
            }
        },
        new EvaluationScenario
        {
            id = "esc-4", skill = "adaptabilidad", skill_label = "Adaptabilidad",
            situation = "El proyecto cambia de dirección de un día para otro por una decisión del cliente. ¿Cómo reaccionas?",
            options = new List<ScenarioOption>
            {
                new() { id = "esc-4-a", text = "Te frustras y cuestionas la decisión abiertamente frente al equipo.", score = 20, feedback = "Es válido sentir frustración, pero exponerla así puede afectar la moral del equipo." },
                new() { id = "esc-4-b", text = "Preguntas el motivo del cambio, ajustas tu plan y sigues adelante.", score = 100, feedback = "¡Excelente actitud! Entender el 'por qué' ayuda a adaptarse mejor y más rápido." },
                new() { id = "esc-4-c", text = "Sigues trabajando en el plan anterior porque ya llevabas avance.", score = 10, feedback = "Ignorar el cambio puede generar trabajo desperdiciado y desalineación." }
            }
        },
        new EvaluationScenario
        {
            id = "esc-5", skill = "liderazgo", skill_label = "Liderazgo",
            situation = "Un integrante del equipo constantemente entrega tarde sus tareas, afectando a los demás. ¿Qué haces?",
            options = new List<ScenarioOption>
            {
                new() { id = "esc-5-a", text = "Ignoras el problema para evitar conflictos.", score = 10, feedback = "Ignorar el problema suele hacerlo crecer y afecta a todo el equipo." },
                new() { id = "esc-5-b", text = "Hablas en privado con la persona para entender qué está pasando y buscar una solución juntos.", score = 100, feedback = "¡Gran enfoque! Abordar el problema con empatía suele dar los mejores resultados." },
                new() { id = "esc-5-c", text = "Lo reportas de inmediato sin conversar primero con la persona.", score = 40, feedback = "Podría ser necesario en algunos casos, pero conversar primero suele ser más efectivo." }
            }
        }
    };

    public static EvaluationScenario? Find(string scenarioId) => All.FirstOrDefault(s => s.id == scenarioId);
}