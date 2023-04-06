# Proyecto Final IDS326-L (ATM Sim)

Este proyecto consiste en varias fases, en las cuales ustedes estar�an trabajando en las pruebas y mantenimiento de 
un "Simulador de ATM".

**Valor total**: 15 puntos
- Fase 2 - Unit Testing (6 pts)
- Fase 3 - Refactoring (3 pts)
- Fase 4 - Features (6 pts)

**Fecha de Entrega**: 22 Abril 2023 11:59:59 pm


**Pautas Generales**:

- Lo primero que har�n es un Fork al proyecto en GitHub para que tengan su propia versi�n para colaborar.

- Cada miembro del equipo debe aportar en cada fase; si un miembro se ve con muy pocos aportes en al repositorio, corre el riesgo
de perder puntos de forma individual.

- Pueden usar la estrategia de branching que m�s les convenga, incluyendo trabajando directo en el main/master branch 
(trunc-based strategy), mientras yo pueda ver que todos los integrantes colaboren.

- La idea es que vayan Fase por Fase. No trabajen en las dem�s fases si no han finalizado la anterior.

Ver los detalles de cada fase a continuaci�n:


## Fase 1 - Experimentaci�n (Entender el proceso y el c�digo)

Normalmente, los casos de pruebas unitarias deber�an ser escritos por el programador que escribi� el c�digo que �stos prueban.
Como ustedes no escribieron esta aplicaci�n, se les complicar� mucho el dise�ar casos de pruebas de calidad, si antes que nada
no se familiarizan con el c�digo y el funcionamiento.

Por lo tanto, el primer paso es que lean el c�digo, y que experimenten, modificando el Program.cs para hacer diferentes
operaciones, ver c�mo funcionan las transacciones, qu� tipo de declinaciones, etc.

Antes de pasar a escribir el primer caso de prueba deben conocer a fondo cada entidad y miembro, como si lo hubiesen escrito
ustedes; si no logran entender, favor de consultarme, en lugar de empezar a trabajar ciegamente en los casos de pruebas.

Una vez conozcan en detalle c�mo funciona todo y se sientan c�modos como usuario del API, procedan con la siguiente fase.


## Fase 2 - Unit Testing (Creaci�n de Casos de Pruebas Unitarias)

Esta Fase consiste en crear casos de pruebas unitarias para los distintos comportamientos que se considere relevante probar.

Todos los primeros commit que realicen deben estar orientados a esto; simult�neamente todos los miembros del equipo
deben escribir casos de pruebas. Por lo tanto, cualquier cambio que hagan al Program.cs durate la Fase 1, traten de no 
incluirlo en commit al master.

**Deber�n al menos tener 12 diferentes casos de pruebas entre todos**, pero lo importante es que cubran los escenarios importantes
de las siguientes entidades:
- ATM
- ATMSwitch
- Autorizador
- Tarjeta
- (Para el HSM no es requerido pero pueden hacerlo si quieren)

Favor de no perder el tiempo con casos de pruebas triviales (probando cosas a muy bajo nivel, o detalles de implementaci�n,
o probando el "lenguaje de programaci�n" o el framework, etc.), recuerden que lo importante es probar l�gica de negocio.

Cuando entiendan que ya cubrieron suficiente del c�digo y que est�n protegidos, pueden pasar a la pr�xima parte.

### Ejemplos Incluidos

Si se fijan en el proyecto ATMSimTests, ya hay varias cosas que les estoy proporcionando, para probar cada una de las entidades:

- **AtmTests**: casos de ejemplo que prueban desde el atm realizando la transacci�n y la validaci�n de lo que el atm realiz� al
recibir la respuesta. Al ejecutar este flujo tambi�n se est� ejecutando el comportamiento de ATMSwitch, Autorizador y HSM. 
Para evaluar el resultado de lo que hizo el ATM se utiliza un Stub llamado FakeConsoleWriter

- **AtmSwitchTests**: casos que prueban el flujo desde el ATMSwitch hacia el HSM y el Autorizador. En este caso, el ATM se sustituye
por un Stub llamado FakeATM. Por lo tanto, puse un helper method llamado Encriptar que permite encriptar el pin a partir de
una llave en claro, como lo hace el ATM (ya que el ATMSwitch espera un criptograma de un PIN, y no el PIN en claro)

- **AuthorizerTests**: casos que prueban directamente el flujo del Autorizador (con el HSM). En este caso, no se prueba ni el ATM ni
el ATMSwitch.

- **FakeATM**: este stub simplemente se utiliza en los casos de pruebas ATMSwitchTests ya que el ATMSwitch requiere una instancia de IATM
s�lo para obtener el nombre.

- **FakeConsoleWriter**: este stub es un Wrapper para la clase Console, para evitar que el ATM y el ATMSwitch utilicen el 
WriteLine/Write/ForegroundColor/BackgroundColor/ResetColor; este stub recibe las llamadas a esos m�todos sin hacer nada m�s que almacenar el 
input recibido y permite luego consultar todo lo que escribieron como si lo estuviesen escribiendo en la consola. Con esto, se puede
evaluar el resultado de una transacci�n en un ATM, usando Asserts, lo cual ser�a complicado si se escribiese en la terminal.

- **FakeThreadSleeper**: este stub no hace nada. S�lo evita que el ATM est� llamando a Thread.Sleep() para evitar que los casos de
pruebas tarden m�s de la cuenta por eso.


## Fase 3 - Refactoring (Aplicar cambios al c�digo sin cambiar el comportamiento)

Ya protegidos con los casos de pruebas, proceder�n a aplicar t�cnicas de refactoring, con la limitante de que cada t�cnica que apliquen debe 
estar identificada en el commit por el nombre que tiene en el cat�logo de [Refactoring Guru](https://refactoring.guru/refactoring/techniques), o de lo contrario
no la tendr� en cuenta.

**Se requiere al menos 10 refactorings entre todos**, a�n mejor si realizan refactorings que mitiguen code smells reales que hayan encontrado, 
pero incluso si no encuentran muchos code smells, pueden inventarse razones para hacer refactorings, mientras apliquen una de las t�cnicas del 
cat�logo y la identifiquen en el commit;

Pueden hacer m�ltiples refactorings por commit, pero deben poner cada uno identificado en el comentario del commit. Por ejemplo:

> Change Value to Reference  
Replace Magic Number with Symbolic Constant

Mejor a�n sin 

Recuerden, en esta fase no deber�an cambiar el comportamiento ni el significado del c�digo, s�lo aplicarle transformaciones que mantengan
la escencia del mismo.

Igual, traten de participar todos en esta fase;


## Fase 4 - Features (Implementar nuevas funcionalidades y mejoras)

Consiste en implementar nuevas funcionalidades y mejoras al programa, seg�n los requerimientos proporcionados debajo, pero a�n m�s importante: 
escribir nuevos casos de pruebas para probar los nuevos cambios implementados por ustedes.

**Deber�n implementar al menos 3 de estos requerimientos**; una excepci�n es que si encuentran un error en el c�digo y lo corrigen, y me
notifican, lo contar� como uno de los 3 requerimientos, pero igual tendr�an que implementar 2 m�s, sin importar cuantos otros errores encuentren.

Algo importante es que deben identificar los commit con el n�mero del ticket que est�n trabajando. Si usan branches, tambi�n deber�an identificarlos 
con el ticket (recomiendo usar branches para esto, pues facilita la experimentaci�n). 

Nota: Esta fase es posiblemente la m�s complicada, as� que por favor no proceder en esta parte hasta que se sientan totalmente c�modos con el c�digo 
y hayan trabajado en las dos anteriores. A diferencia de las dem�s Fases, para esta no requerir� necesariamente la colaboraci�n de todos los integrantes,
pero igual se recomienda.


### Requerimientos:

#### Easy:

- **RQ01-Montos Decimales**: Permitir retiros de hasta 2 d�gitos decimales. Realizar los ajustes necesarios al c�digo para que se acepten 
montos y balances en decimales, manteniendo hasta dos d�gitos

- **RQ02-Relaci�n de Tarjetas y Cuentas**: Actualmente, las tarjetas tienen el n�mero de cuenta almacenado como un atributo. En el mundo real, 
es el autorizador que guarda la relaci�n entre Tarjetas y Cuentas, por lo que se debe implementar un cambio para que el autorizador tenga una 
estructura de datos con la relaci�n Tarjeta->Cuenta, y al recibir una transacci�n, consulte en esta estructura para determinar la cuenta.

#### Medium:

- **RQ03-Limite de Sobregiro Cuenta Corriente**: Actualmente, las cuentas corrientes se permiten sobregirar por cualquier cantidad. Se 
requiere implementar un par�metro que se configure en la creaci�n de la cuenta, y que espcifique el l�mite de sobregiro, y si se excede este l�mite 
se declinar�a con fondos insuficientes.

- **RQ04-Bloqueo de Tarjetas**: Implementar una estructura de "estado" de las tarjetas en el Autorizador, que permita bloquear la tarjeta 
y que las transacciones declinen si la tarjeta est� bloqueada. Esta declinaci�n deber� mostrar un error en la pantalla del ATM indicando esto.

- **RQ05-L�mite de Retiro**: Implementar un l�mite para las transacciones de retiro, configurable por Autorizador. El l�mite ser� por transacci�n,
lo que quiere decir que no se requiere un "acumulador" o mantener alg�n conteo entre transacci�n y transacci�n. Cuando se exceda el l�mite de retiro, 
deber� mostrar una pantalla de error indicando �sto.

#### Hard:

- **RQ06-Dispensaciones parciales**: Implementar un contador del efectivo para el ATM; al crear el ATM se le especifica el balance del ATM
y se descuenta por cada retiro. Al realizar un retiro, si el balance del ATM no es suficiente para completar el monto del retiro, el ATM dispensar�
"lo que le queda", y enviar� el TransactionRequest s�lo por el monto que puede dispensar. Al final de la transacci�n indicar� al cliente que s�lo pudo
dispensar X cantidad.

- **RQ07-Nueva Transacci�n: Dep�sitos**: Implementar una nueva transacci�n de Dep�sitos (con su propia combinaci�n de teclas), la cual ser� lo 
contrario del retiro, permitiendo agregarle dinero a una cuenta al realizar uno. Al realizar el dep�sito el ATM debe mostrar "> Efectivo Depositado: XXXXX", 
por lo que se requiere un nuevo tipo de Comando de respuesta

- **RQ08-Nueva Transacci�n: Cambio de PIN**: Implementar un nuevo tipo de transacci�n de "cambio de PIN" (con su propia combinaci�n de teclas) 
que requerir� el pin anterior y el pin nuevo. Si el pin anterior est� incorrecto, se declina, indicando la raz�n en pantalla, y no se aplica el cambio 
de PIN. Si el pin anterior se ingresa correcto, deber� realizarse el "cambio de PIN", y debe indicar que fue satisfactorio en la pantalla. Las futuras 
transacciones con esa misma tarjeta s�lo autorizar�an con el nuevo PIN. S�lo se deben soportar pines de 4 d�gitos num�ricos

#### Challenge:

- **RQ09-Cargo de Retiro Internacional**: [*very hard*] Implementar un "cargo de retiro con tarjeta Internacional" que se configure por bin de la tarjeta 
(bin=primeros 6 d�gitos de la tarjeta). El ATMswitch deber� mantener una estructura con los bines que aplican para este cargo, y el monto del cargo para 
cada uno. Al recibir un TransactionRequest del ATM para una tarjeta que aplica para este cargo, el ATMSwitch deber� enviarle un(os) Comando(s) 
(requiere la creaci�n de un nuevo comando que permita responder s� o no) al ATM para mostrar "Para este retiro se le aplicar� un cargo de RD$XXX", y 
cuando el usuario acepte, el ATM enviar� la transacci�n nuevamente con el monto del cargo sumado al monto del retiro. Entonces el ATMSwitch proceder� 
a autorizar con el Autorizador normalmente, pero si se autoriza el Retiro, cuando el Switch env�e los comandos, en el ComandoDispensarEfectivo deber� 
mostrarse el monto sin el cargo (porque se supone que el cargo se cobra al cliente, pero no se le dispensa en efectivo). Esto requiere muchas modificaciones 
y varios casos de pruebas nuevos, as� que s�lo intentar este si tienen tiempo de sobra


# Buena suerte!