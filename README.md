# Prueba de Concepto - Implementación del Patrón CQRS con Microservicios

## Descripción de la Prueba
Esta prueba de concepto implementa el patrón CQRS (Command Query Responsibility Segregation) utilizando microservicios en .NET 8 para gestionar las operaciones de escritura y lectura en una aplicación de gestión de películas. El proyecto incluye dos microservicios: uno para los comandos (operaciones de escritura) y otro para las consultas (operaciones de lectura).

A continuación se muestra un diagrama que ilustra la arquitectura general de la implementación:

![Diagrama CQRS](assets/diagrama-cqrs.svg)

## Objetivo(s) de la Prueba
- Demostrar el uso del patrón CQRS mediante la separación de los comandos y consultas en microservicios distintos.
- Evaluar la integración de distintas tecnologías y bibliotecas, como MediatR para la gestión de comandos y consultas, MassTransit para la mensajería entre microservicios, y Elasticsearch y Redis para el almacenamiento y cacheo de datos.
- Validar la integración de SQL Server y Elasticsearch en las operaciones de comandos.
- Verificar el rendimiento y la eficiencia de cacheo en consultas mediante Redis y el registro de logs en Elasticsearch.

## Pasos Implementados para Llevar a Cabo la Prueba
1. **Configuración de Microservicios**: 
   - Se crearon dos microservicios en .NET 8:
     - **Microservicio de Commands**: Gestiona las operaciones `Delete`, `Update`, y `Create` para las entidades de películas. Incluye un endpoint de migración, únicamente para la prueba de concepto, que permite añadir datos iniciales tanto en SQL Server como en Elasticsearch. Este endpoint no estaría presente en un entorno de producción.
     - **Microservicio de Queries**: Contiene tres endpoints de consulta (`GetAll`, `GetByMovieId`, y `GetMovieByQuery`) para recuperar información de películas. Este microservicio cachea los resultados en Redis y registra los logs de las operaciones en Elasticsearch.
   
2. **Comunicación entre Microservicios**:
   - La comunicación entre los microservicios se realiza a través de **RabbitMQ**, utilizando la biblioteca **MassTransit** para la mensajería.

3. **Infraestructura en Docker**:
   - La infraestructura del proyecto fue definida en un archivo `docker-compose.yml`, que permite inicializar todos los servicios necesarios, incluyendo SQL Server, Redis, Elasticsearch, y RabbitMQ, además de los contenedores de los dos microservicios.

## Tecnologías Usadas en la Prueba
- **Lenguaje y Frameworks**: 
  - C# y .NET 8
- **Bases de Datos**:
  - SQL Server para la persistencia de datos estructurados.
  - Elasticsearch para búsquedas avanzadas y almacenamiento de logs.
  - Redis para el cacheo de resultados en el microservicio de consultas.
- **Mensajería**:
  - RabbitMQ para la comunicación entre microservicios.
- **Bibliotecas**:
  - **MediatR**: Para la implementación del patrón CQRS y gestión de comandos y consultas.
  - **MassTransit**: Para integrar RabbitMQ en los microservicios.
  - **AutoMapper**: Para el mapeo de entidades y DTOs.
  - **Polly**: Para la implementación de resiliencia y manejo de fallos.
  - **Serilog**: Para el manejo y almacenamiento de logs.
- **Infraestructura**:
  - Docker Compose para la orquestación de los contenedores y despliegue de toda la infraestructura.

## Resultados
- **Separación de Responsabilidades**: La implementación del patrón CQRS permitió la separación de responsabilidades de lectura y escritura en dos microservicios independientes, mejorando la organización y escalabilidad del código.
- **Carga Inicial de Datos**: El endpoint de migración se usó solo para la carga inicial de datos en SQL Server y Elasticsearch durante la prueba, y no se usaría en producción.
- **Cacheo y Rendimiento**: El uso de Redis para cachear las consultas resultó en un aumento significativo en el rendimiento de las operaciones de lectura.
- **Manejo de Logs**: Los logs se almacenaron adecuadamente en Elasticsearch, permitiendo un seguimiento detallado de las actividades de cada microservicio.
- **Mensajería Asíncrona**: La comunicación asíncrona entre microservicios a través de RabbitMQ funcionó de manera confiable, facilitando la independencia entre los servicios de comandos y consultas.

## Conclusiones
La prueba de concepto demostró que el patrón CQRS, junto con una arquitectura de microservicios y la integración de tecnologías de mensajería y almacenamiento en caché, es altamente efectivo para aplicaciones con operaciones de lectura y escritura intensivas. La separación en microservicios de comandos y consultas no solo simplifica la gestión del código, sino que permite una escalabilidad y un rendimiento óptimos. Integrar Redis para el cacheo y Elasticsearch para la búsqueda y logging agrega flexibilidad y eficiencia, mientras que la comunicación mediante RabbitMQ garantiza la coordinación de eventos entre microservicios sin crear dependencias fuertes.
