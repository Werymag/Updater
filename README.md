<h1 align="center">Встраиваемый в приложение модуль обновления программы</h1>

Работает в паре с сервером обновлений:

Модуль встраивается в программу и запускается из неё с двумя аргументами командной строки:
	1. Путём к исполняемому файлу обновляемой программы;
	2. Url адресу сервера обновлений.

Работает по следующему принципу:
	- Получает с сервера обновлений актуальный номер версии программы;
	- Получает версию программы из исполняемого файла первого аргумента командной строки;
	- В случае необходимости обновления получает список файлов программы с MD5 хеш суммами;
	- Создаёт копию файлов текущей версии во временный каталог {User}/AppData/Roaming/{Program Name}/{Version};
	- Копирует актуальные версии файлов текущей версии во временный каталог {User}/AppData/Roaming/{Program Name}/{Version} докачивая неактуальные и отсутствующие;
	- После успешного скачивания пытается удалить все файлы текущей версии и записать на их место файлы новой версии;
	- В случае неудачи на любом этапе откатывает файлы в исходное состояние.


Для корректного обновления перед запуском необходимо скопировать исполняемый модуль средствами обновляемой программы в любой разрешенный каталог, оптимально - {User}/AppData/Roaming/{Program Name}.


В src приложено два варианта исполнения модуля, в виде консольного приложения и окна WPF с простым интерфейсом, которые необходимо скомпилировать и добавить к приложению как "содержимое"".