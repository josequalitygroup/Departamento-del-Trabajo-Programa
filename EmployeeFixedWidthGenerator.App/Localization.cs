namespace EmployeeFixedWidthGenerator.App;

internal enum AppLanguage
{
    English,
    Spanish
}

internal static class Localization
{
    public static string T(string key, AppLanguage language)
    {
        bool es = language == AppLanguage.Spanish;
        return key switch
        {
            "AppTitle" => es ? "Cargador del Departamento del Trabajo" : "Department of Labour Uploader",
            "HeaderTitle" => es ? "Cargador del Departamento del Trabajo de Jose" : "Jose's Department of Labour Uploader",
            "HeaderSubtitle" => es ? "Genere registros de nómina de 150 caracteres con validaciones estrictas." : "Generate compliant 150-character payroll records with strict validations.",
            "InputMode" => es ? "Modo de Entrada" : "Input Mode",
            "UseTemplate" => es ? "Usar plantilla de Excel" : "Use Excel template",
            "ManualEntry" => es ? "Ingresar empleados en la app" : "Enter employees in app",
            "TemplateUpload" => es ? "Carga de Plantilla Excel" : "Excel Template Upload",
            "ManualEmployeeEntry" => es ? "Ingreso Manual de Empleados" : "Manual Employee Entry",
            "ActionsOutput" => es ? "Acciones y Salida" : "Actions & Output",
            "RecordPreview" => es ? "Vista Previa del Registro" : "Record Preview",
            "Status" => es ? "Estado y Mensajes" : "Status & Messages",
            "GenerateTxt" => es ? "Generar TXT" : "Generate TXT",
            "PreviewFirst" => es ? "Previsualizar Primer Registro" : "Preview First Record",
            "Browse" => es ? "Examinar..." : "Browse...",
            "GenerateTemplate" => es ? "Generar Plantilla" : "Generate Template",
            "OutputFolder" => es ? "Carpeta de salida:" : "Output folder:",
            "OutputFilename" => es ? "Archivo de salida:" : "Output filename:",
            "Year" => es ? "Año:" : "Year:",
            "Quarter" => es ? "Trimestre:" : "Quarter:",
            "Batch" => es ? "Lote (6):" : "Batch Number (6):",
            "Trailing" => es ? "Incluir línea vacía final" : "Include trailing empty line",
            "Language" => es ? "Idioma:" : "Language:",
            "TemplateSuccess" => es ? "Plantilla creada correctamente." : "Template created successfully.",
            "LoginTitle" => es ? "Inicio de Sesión" : "Login",
            "User" => es ? "Usuario" : "User",
            "Password" => es ? "Contraseña" : "Password",
            "Login" => es ? "Ingresar" : "Login",
            "InvalidCredentials" => es ? "Credenciales inválidas." : "Invalid credentials.",
            "LockedTryLater" => es ? "Demasiados intentos. Intente nuevamente en {0} segundos." : "Too many attempts. Try again in {0} seconds.",
            "SplashTitle" => "Jose's Department of Labour Uploader",
            _ => key
        };
    }
}
