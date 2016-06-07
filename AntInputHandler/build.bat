@echo off
if exist ant.jar (
	dir /s /B *.java > sources.txt
	if not exist bin mkdir bin
	javac -classpath ant.jar -d bin @sources.txt
	del sources.txt
	cd bin
	jar -cf ant-input.jar org
	echo Place the `ant-input.jar` file in the `lib` folder of your `ant` directory.
) else (
	echo Could not find `ant.jar`. Please copy it from the `lib` folder of your `ant` directory.
)
pause
