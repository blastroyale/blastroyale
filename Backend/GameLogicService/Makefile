

build:
	cd .. && docker build -t game-logic-service-local --no-cache -f  GameLogicService/Dockerfile .

run:
	docker run -d -p 80:80 game-logic-service-local