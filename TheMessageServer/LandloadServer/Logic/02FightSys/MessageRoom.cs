﻿//风声房间

using PEProtocol;
using System;
using System.Collections.Generic;
using System.Text;


public class MessageRoom
{
    public int RoomID { private set; get; }
    public MessagePlayer[] playerArr = null;
    public RoomState roomState = RoomState.None;
    public RoundStage roundStage = RoundStage.None;
    public string roomOwner = null;
    public int roomOwnerID;
    public MatchPlayerData[] matchPlayerArr = null;

    List<Card> cardList = new List<Card>();//牌库
    List<Card> tempCardList = new List<Card>();//创建一个临时牌库用来存放打乱前的牌
    public int roomNumber { private set; get; }

    private int readyNumber;//准备的人数
    private int charCount;//已选择角色的人数

    public int roundPlayerIndex { private set; get; }//正在进行回合的玩家索引

    public Card transferingMessage { private set; get; } = null;//传递中的情报
    public int transferingMessageIndex { private set; get; } = -1;//当前面前有情报的人的索引
    private Card waitSettlementCard = null;//等待结算的卡牌
    private bool settlementCardAvailability = false;//结算卡牌的有效性
    private int settlementCardTarget = -1;//结算卡牌的目标
    private int settlementCardUser = -1;//结算卡牌使用者的索引
    private Card waitBurnCard = null;//等待烧毁的卡牌

    public MessageRoom(int roomID,string roomOwner,int roomOwnerID)
    {
        RoomID = roomID;
        this.roomOwner = roomOwner;
        this.roomOwnerID = roomOwnerID;
        playerArr = new MessagePlayer[5];
        roomState = RoomState.Matching;
        matchPlayerArr = new MatchPlayerData[5];
        roomNumber = 0;
        readyNumber = 0;
        charCount = 0;
        roundPlayerIndex = 0;
    }

    //添加玩家
    public void AddMessagePlayer(MessagePlayer messagePlayer, int posIndex)
    {
        playerArr[posIndex] = messagePlayer;
        MatchPlayerData matchPlayerData = new MatchPlayerData
        {
            id = messagePlayer.id,
            name = messagePlayer.name,
            posIndex = messagePlayer.posIndex,
            iconIndex = messagePlayer.iconIndex,
            isReady = false
        };

        matchPlayerArr[posIndex] = matchPlayerData;
        roomNumber++;
        SyncRoomInfo();
    }
    //玩家退出房间
    public void ExitMessagePlayer(int id)
    {
        int posIndex = GetIndex(id);
        if (posIndex >= 0)
        {
            matchPlayerArr[posIndex] = null;
            playerArr[posIndex] = null;

            roomNumber--;

            for (int i = posIndex+1; i < matchPlayerArr.Length; i++)
            {
                matchPlayerArr[i-1] = matchPlayerArr[i];
                playerArr[i - 1] = playerArr[i];
            }
            matchPlayerArr[matchPlayerArr.Length-1] = null;
            playerArr[playerArr.Length-1] = null;
        }

    }
    //同步房间信息
    private void SyncRoomInfo()
    {
        GameMsg msg = new GameMsg
        {
            cmd = CMD.PshMatch,
            pshMatch = new PshMatch
            {
                RoomID = RoomID,
                playerArr = matchPlayerArr
            }
        };

        for (int i = 0; i < playerArr.Length; i++)
        {
            MessagePlayer player = playerArr[i];
            if (player != null)
            {
                msg.pshMatch.selfPosIndex = player.posIndex;
                player.token.SendMsg(msg);
            }
        }

    }

    public int GameReady(int id)//返回准备者的索引号用于给房间中所有玩家推送准备消息
    {
        int posIndex = -1;
        for (int i = 0;i< matchPlayerArr.Length; i++)
        {
            if(matchPlayerArr[i]!= null) {
                if (id == matchPlayerArr[i].id&&matchPlayerArr[i].isReady == false)
                {
                    matchPlayerArr[i].isReady = true;
                    posIndex = i;
                    readyNumber++;
                }
            }

        }
        return posIndex;
    }

    public int CancelReady(int id)//取消准备
    {
        int posIndex = -1;
        for (int i = 0; i < matchPlayerArr.Length; i++)
        {
            if (matchPlayerArr[i] != null)
            {
                if (id == matchPlayerArr[i].id && matchPlayerArr[i].isReady == true)
                {
                    matchPlayerArr[i].isReady = false;
                    posIndex = i;
                    readyNumber--;
                }
            }

        }
        return posIndex;
    }
    //判断玩家是否全部准备
    public bool AllReady()
    {
        this.ColorLog(PEUtils.LogColor.Yellow, "ReadyNumber:{0}", readyNumber);
        if(readyNumber == playerArr.Length - 1)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    //根据玩家信息获取索引
    public int GetIndexById(int id)
    {
        int posIndex = -1;
        for(int i = 0; i < playerArr.Length; i++)
        {
            if(playerArr[i].id == id)
            {
                posIndex = i;
            }
        }
        return posIndex;
    }


    //游戏开始房间初始化
    public void GameStart()
    {
        //改变房间状态
        roomState = RoomState.Matching;
        //初始化玩家状态
        for(int i = 0; i < playerArr.Length; i++)
        {
            playerArr[i].InitMatch();
        }



    }

    //更新数据
    public void UpdateMatchData()
    {
        for(int i = 0; i < playerArr.Length; i++)
        {
            matchPlayerArr[i].cards = playerArr[i].cardList.Count;
            matchPlayerArr[i].redNum = playerArr[i].redNum;
            matchPlayerArr[i].blueNum = playerArr[i].blueNum;
            matchPlayerArr[i].blackNum = playerArr[i].blackNum;
            matchPlayerArr[i].charIndex = playerArr[i].charIndex;
            matchPlayerArr[i].charName = playerArr[i].charName;
        }
    }
    //TOOL METHONDS
    public int GetIndex(int id)
    {
        int posIndex = -1;
        for (int i = 0; i < matchPlayerArr.Length; i++)
        {
            if (matchPlayerArr[i] != null)
            {
                if (id == matchPlayerArr[i].id)
                {
                    
                    posIndex = i;
                    break;

                }
            }

        }

        return posIndex;
    }

    public bool SetPlayerChar(int id, int index, string name)
    {
        bool flag = false;
        int playerIndex = GetIndex(id);
        playerArr[playerIndex].charIndex = index;
        playerArr[playerIndex].charName = name;


        charCount++;


        if (charCount == playerArr.Length)
        {
            flag = true;
        }

        return flag;
    }

    //随机生成身份
    public void RandomIdentity()
    {
        List<int> list = new List<int>();
        Random rand = new Random();
        int k = 0;
        do
        {
            k = rand.Next(1, 6);
            if (!list.Contains(k))
                list.Add(k);
        }
        while (list.Count < 5);

        for(int i = 0; i < playerArr.Length; i++)
        {
            playerArr[i].SetIdentity(list[i]);
        }
    }

    //创建牌
    public void CreatRandomCard()
    {
        

        

        CreatCard(CardColor.Black, CardType.NonstopMessage, CardFunction.Gambling, 2);//2 黑直达博弈
        CreatCard(CardColor.Black, CardType.TextMessage, CardFunction.Swap, 2);//2 黑文本调包
        CreatCard(CardColor.Red, CardType.TextMessage, CardFunction.Swap, 2);//2 红文本调包
        CreatCard(CardColor.RedBlack, CardType.TextMessage, CardFunction.Swap, 1);//1 红黑文本掉包
        CreatCard(CardColor.Blue, CardType.TextMessage, CardFunction.Swap, 2);//2 蓝文本掉包
        CreatCard(CardColor.BlueBlack, CardType.TextMessage, CardFunction.Swap, 1);//1 蓝黑文本调包
        CreatCard(CardColor.Black, CardType.RestrictedMessage, CardFunction.TigerMountain, 2);//2 黑密电调虎
        CreatCard(CardColor.Red, CardType.RestrictedMessage, CardFunction.TigerMountain, 4);//4 红密电调虎
        CreatCard(CardColor.RedBlack, CardType.RestrictedMessage, CardFunction.TigerMountain, 2);//2 红黑密电调虎
        CreatCard(CardColor.Blue, CardType.RestrictedMessage, CardFunction.TigerMountain, 4);//4 蓝密电调虎
        CreatCard(CardColor.BlueBlack, CardType.RestrictedMessage, CardFunction.TigerMountain, 2);//2 蓝黑密电调虎
        CreatCard(CardColor.Black, CardType.TextMessage, CardFunction.PublicDocument, 1);//1 黑文本公开文档 
        CreatCard(CardColor.Red, CardType.TextMessage, CardFunction.PublicDocument, 1);//1 红文本公开文档
        CreatCard(CardColor.Blue, CardType.TextMessage, CardFunction.PublicDocument, 1);//1 蓝文本公开文档
        CreatCard(CardColor.Black, CardType.NonstopMessage, CardFunction.Intercept, 5);//5 黑直达截获
        CreatCard(CardColor.Red, CardType.NonstopMessage, CardFunction.Intercept, 2);//2 红直达截获
        CreatCard(CardColor.RedBlack, CardType.NonstopMessage, CardFunction.Intercept, 1);//1 红黑直达截获
        CreatCard(CardColor.Blue, CardType.NonstopMessage, CardFunction.Intercept, 2);//2 蓝直达截获
        CreatCard(CardColor.BlueBlack, CardType.NonstopMessage, CardFunction.Intercept, 1);//1 蓝黑直达截获
        CreatCard(CardColor.Black, CardType.RestrictedMessage, CardFunction.Decipher, 2);//2 黑密电破译
        CreatCard(CardColor.Red, CardType.RestrictedMessage, CardFunction.Decipher, 2);//2 红密电破译
        CreatCard(CardColor.RedBlack, CardType.RestrictedMessage, CardFunction.Decipher, 1);//1 红黑密电破译
        CreatCard(CardColor.Blue, CardType.RestrictedMessage, CardFunction.Decipher, 2);//2 蓝密电破译
        CreatCard(CardColor.BlueBlack, CardType.RestrictedMessage, CardFunction.Decipher, 1);//1 蓝黑密电破译
        CreatCard(CardColor.Black, CardType.NonstopMessage, CardFunction.Balance, 2);//2 黑直达权衡
        CreatCard(CardColor.Black, CardType.NonstopMessage, CardFunction.Burn, 3);//3 黑直达烧毁
        CreatCard(CardColor.Red, CardType.NonstopMessage, CardFunction.Burn, 2);//2 红直达烧毁
        CreatCard(CardColor.Blue, CardType.NonstopMessage, CardFunction.Burn, 2);//2 蓝直达烧毁
        CreatCard(CardColor.Black, CardType.NonstopMessage, CardFunction.Penetrate, 5);//5 黑直达识破
        CreatCard(CardColor.Red, CardType.NonstopMessage, CardFunction.Penetrate, 2);//2 红直达识破
        CreatCard(CardColor.RedBlack, CardType.NonstopMessage, CardFunction.Penetrate, 1);//1 红黑直达识破
        CreatCard(CardColor.Blue, CardType.NonstopMessage, CardFunction.Penetrate, 2);//2 蓝直达识破
        CreatCard(CardColor.BlueBlack, CardType.NonstopMessage, CardFunction.Penetrate, 1);//1 蓝黑直达识破
        CreatCard(CardColor.Black, CardType.RestrictedMessage, CardFunction.ProbingSoySauce_1, 1);//1 黑密电试探酱油+1
        CreatCard(CardColor.Black, CardType.RestrictedMessage, CardFunction.ProbingSoySauce_0, 1);//1 黑密电试探酱油-1
        CreatCard(CardColor.Red, CardType.RestrictedMessage, CardFunction.ProbingSoySauce_1, 1);//1 红密电试探酱油+1
        CreatCard(CardColor.Red, CardType.RestrictedMessage, CardFunction.ProbingSoySauce_0, 1);//1 红密电试探酱油-1
        CreatCard(CardColor.Blue, CardType.RestrictedMessage, CardFunction.ProbingSoySauce_1, 1);//1 蓝密电试探酱油+1
        CreatCard(CardColor.Blue, CardType.RestrictedMessage, CardFunction.ProbingSoySauce_0, 1);//1 蓝密电试探酱油-1

        CreatCard(CardColor.Black, CardType.RestrictedMessage, CardFunction.ProbingMilitary_1, 1);//1 黑密电试探军情+1
        CreatCard(CardColor.Black, CardType.RestrictedMessage, CardFunction.ProbingMilitary_0, 1);//1 黑密电试探军情-1
        CreatCard(CardColor.Red, CardType.RestrictedMessage, CardFunction.ProbingMilitary_1, 1);//1 红密电试探军情+1
        CreatCard(CardColor.Red, CardType.RestrictedMessage, CardFunction.ProbingMilitary_0, 1);//1 红密电试探军情-1
        CreatCard(CardColor.Blue, CardType.RestrictedMessage, CardFunction.ProbingMilitary_1, 1);//1 蓝密电试探军情+1
        CreatCard(CardColor.Blue, CardType.RestrictedMessage, CardFunction.ProbingMilitary_0, 1);//1 蓝密电试探军情-1

        CreatCard(CardColor.Black, CardType.RestrictedMessage, CardFunction.ProbingLurker_1, 1);//1 黑密电试探潜伏+1
        CreatCard(CardColor.Black, CardType.RestrictedMessage, CardFunction.ProbingLurker_0, 1);//1 黑密电试探潜伏-1
        CreatCard(CardColor.Red, CardType.RestrictedMessage, CardFunction.ProbingLurker_1, 1);//1 红密电试探潜伏+1
        CreatCard(CardColor.Red, CardType.RestrictedMessage, CardFunction.ProbingLurker_0, 1);//1 红密电试探潜伏-1
        CreatCard(CardColor.Blue, CardType.RestrictedMessage, CardFunction.ProbingLurker_1, 1);//1 蓝密电试探潜伏+1
        CreatCard(CardColor.Blue, CardType.RestrictedMessage, CardFunction.ProbingLurker_0, 1);//1 蓝密电试探潜伏-1

        CreatCard(CardColor.Red, CardType.RestrictedMessage, CardFunction.Locking, 6);//6 红密电锁定
        CreatCard(CardColor.RedBlack, CardType.RestrictedMessage, CardFunction.Locking, 3);//3 红黑密电锁定
        CreatCard(CardColor.Blue, CardType.RestrictedMessage, CardFunction.Locking, 6);//6 蓝密电锁定
        CreatCard(CardColor.BlueBlack, CardType.RestrictedMessage, CardFunction.Locking, 3);//3 蓝黑锁定密电
        CreatCard(CardColor.Black, CardType.RestrictedMessage, CardFunction.Reinforce, 2);//2 黑密电增援
        CreatCard(CardColor.Red, CardType.RestrictedMessage, CardFunction.Reinforce, 1);//1 红密电增援
        CreatCard(CardColor.Blue, CardType.RestrictedMessage, CardFunction.Reinforce, 1);//1 蓝密电增援
        CreatCard(CardColor.Red, CardType.TextMessage, CardFunction.RealOrFalse, 1);//1 红文本真伪莫辨
        CreatCard(CardColor.Blue, CardType.TextMessage, CardFunction.RealOrFalse, 1);//1 蓝文本真伪莫辨
        CreatCard(CardColor.Black, CardType.NonstopMessage, CardFunction.Transfer, 1);//1 黒直达转移
        CreatCard(CardColor.Red, CardType.NonstopMessage, CardFunction.Transfer, 200);//2 红直达转移
        CreatCard(CardColor.Blue, CardType.NonstopMessage, CardFunction.Transfer, 2);//2 蓝直达转移

        //打乱牌序
        cardList.Clear();
        Random rand = new Random();
        for (int i = 0; i < tempCardList.Count; i++)
        {
            int index = rand.Next(0, cardList.Count + 1);
            cardList.Insert(index, tempCardList[i]);
        }

    }

    public void CreatCard(CardColor mColor, CardType mType, CardFunction mFunction,int count)
    {
        Card card = new Card
        {
            color = mColor,
            type = mType,
            function = mFunction
        };
        for(int i = 0; i < count; i++)
        {
            tempCardList.Add(card);
        }
    }

    //将牌分发给每个玩家
    public void DispenseCardToPlayer()
    {
        //清空玩家手里的牌

        for(int i = 0; i < playerArr.Length; i++)
        {
            playerArr[i].cardList.Clear();
        }



        //分发手牌和底牌，牌库已打乱，所以直接按顺序发

        for(int i = 0; i < playerArr.Length; i++)
        {
            for(int j = 0;j < 4; j++)
            {
                playerArr[i].AddCard(cardList[0]);
                cardList.RemoveAt(0);
            }
        }

        

        //推送所有牌的信息给客户端
        GameMsg msg = new GameMsg
        {
            cmd = CMD.PushCard,
            pushCard = new PushCard{}
        };
        for (int i = 0; i < playerArr.Length; i++)
        {

            msg.pushCard.cardList = playerArr[i].cardList;

            playerArr[i].token.SendMsg(msg);//发到对应的客户端

        }

    }

    public void RemoveCard(int index,Card card)
    {

        for (int i = 0; i < playerArr[index].cardList.Count; i++)
        {
            if (playerArr[index].cardList[i].Equals(card))
            {
                playerArr[index].cardList.RemoveAt(i);
                break;
            }
        }
        GameMsg updateMsg = new GameMsg
        {
            cmd = CMD.PushSinglePlayerMessageUpdate,
            pushSinglePlayerMessageUpdate = new PushSinglePlayerMessageUpdate
            {
                posIndex = index,
                cards = playerArr[index].cardList.Count,
                redNum = playerArr[index].redNum,
                blueNum = playerArr[index].blueNum,
                blackNum = playerArr[index].blackNum,
                cardLibraryCount = cardList.Count
            }
        };

        CacheSvc.Instance.SendMsgAll(this, updateMsg);
    }

    public void RemoveMessage(int index,Card message)
    {
        playerArr[index].RemoveMessage(message);
        GameMsg updateMsg = new GameMsg
        {
            cmd = CMD.PushSinglePlayerMessageUpdate,
            pushSinglePlayerMessageUpdate = new PushSinglePlayerMessageUpdate
            {
                posIndex = index,
                cards = playerArr[index].cardList.Count,
                redNum = playerArr[index].redNum,
                blueNum = playerArr[index].blueNum,
                blackNum = playerArr[index].blackNum,
                cardLibraryCount = cardList.Count
            }
        };

        CacheSvc.Instance.SendMsgAll(this, updateMsg);
    }

    //抽卡
    public void DrawCard(int drawIndex,int drawCardCount)
    {
        List<Card> tempCardList = new List<Card>();
        for (int i = 0; i < drawCardCount; i++)
        {
            playerArr[drawIndex].AddCard(cardList[0]);
            tempCardList.Add(cardList[0]);
            cardList.RemoveAt(0);
        }

        GameMsg msg = new GameMsg
        {
            cmd = CMD.PushDrawCard,
            pushDrawCard = new PushDrawCard
            {
                cardList = tempCardList,
                index = drawIndex,
                cardLibraryCount = cardList.Count
            }
        };

        CacheSvc.Instance.SendMsgAll(this, msg);
    }


    //回合开始阶段
    public void RoundStart()
    {
        //TODO

        roundStage = RoundStage.RoundStart;

        GameMsg msg = new GameMsg
        {
            cmd = CMD.PushRoundStart,
            pushRoundStart = new PushRoundStart { index = roundPlayerIndex }
        };
        CacheSvc.Instance.SendMsgAll(this,msg);

        DrawPhase();

    }
    
    //抽牌阶段
    public void DrawPhase()
    {
        DrawCard(roundPlayerIndex, 2);

        PlayerStage();

    }

    //出牌阶段
    public void PlayerStage()
    {
        //TODO

        GameMsg msg = new GameMsg
        {
            cmd = CMD.PushPlayStage
        };
        CacheSvc.Instance.SendMsgAll(this, msg);


        roundStage = RoundStage.PlayStage;

    }

    //情报传递阶段
    public void MessageTransfer()
    {
        //TODO

        roundStage = RoundStage.MessageTransfer;

    }

    //传递小节
    public void TransferSection()
    {
        //TODO

        //暂时废弃
        roundStage = RoundStage.TransferSection;

    }

    //到达小节
    public void ArriveSection()
    {
        //TODO

        roundStage = RoundStage.ArriveSection;

        GameMsg msg = new GameMsg { cmd = CMD.PushArriveSection };

        CacheSvc.Instance.SendMsgAll(this, msg);

    }

    //接受小节
    public void AcceptSection()
    {
        //TODO

        roundStage = RoundStage.AcceptSection;

        GameMsg msg = new GameMsg { cmd = CMD.PushAcceptSection };
        playerArr[transferingMessageIndex].token.SendMsg(msg);

    }

    //回合结束阶段
    public void RoundEnd()
    {
        //TODO

        roundStage = RoundStage.RoundEnd;

        for(int i = 0; i < playerArr.Length; i++)
        {
            playerArr[i].playerState = PlayerState.None;
        }

        GameMsg msg = new GameMsg
        {
            cmd = CMD.PushRoundEnd
        };

        CacheSvc.Instance.SendMsgAll(this,msg);

        roundPlayerIndex++;
        if(roundPlayerIndex > 4)
        {
            roundPlayerIndex = 0;
        }
        RoundStart();
    }

    public void SetTransferMessage(int index, Card card = null)
    {
        if(card != null)
        {
            transferingMessage = card;

        }
        if(index > 4)
        {
            index -= 5;
        }
        transferingMessageIndex = index;
    }

    public void AcceptMessage()
    {
        playerArr[transferingMessageIndex].AddMessage(transferingMessage);


        if(transferingMessage.function == CardFunction.PublicDocument)
        {
            switch (transferingMessage.color)
            {
                case CardColor.Red:
                    if(playerArr[transferingMessageIndex].playerIdentity != PlayerIdentity.Lurker)
                    {
                        DrawCard(transferingMessageIndex, 1);
                        SendProbingMsg(transferingMessageIndex, 1);
                    }
                    break;
                case CardColor.Blue:
                    if (playerArr[transferingMessageIndex].playerIdentity != PlayerIdentity.Military)
                    {
                        DrawCard(transferingMessageIndex, 1);
                        SendProbingMsg(transferingMessageIndex, 1);
                    }
                    break;
                case CardColor.Black:
                    if (playerArr[transferingMessageIndex].playerIdentity != PlayerIdentity.SoySauce)
                    {
                        DrawCard(transferingMessageIndex, 1);
                        SendProbingMsg(transferingMessageIndex, 1);
                    }
                    break;
                default:
                    break;
            }
        }

        GameMsg updateMsg = new GameMsg
        {
            cmd = CMD.PushSinglePlayerMessageUpdate,
            pushSinglePlayerMessageUpdate = new PushSinglePlayerMessageUpdate
            {
                posIndex = transferingMessageIndex,
                cards = playerArr[transferingMessageIndex].cardList.Count,
                redNum = playerArr[transferingMessageIndex].redNum,
                blueNum = playerArr[transferingMessageIndex].blueNum,
                blackNum = playerArr[transferingMessageIndex].blackNum,
                cardLibraryCount = cardList.Count
            }
        };

        CacheSvc.Instance.SendMsgAll(this, updateMsg);



        GameMsg confirmMsg = new GameMsg
        {
            cmd = CMD.PushConfirmAcceptMessage,
            pushConfirmAcceptMessage = new PushConfirmAcceptMessage
            {
                index = transferingMessageIndex
            }
        };
        CacheSvc.Instance.SendMsgAll(this, confirmMsg);

        transferingMessageIndex = -1;
        transferingMessage = null;
        RoundEnd();

    }

    //设置等待结算的卡牌
    public void SetWaitSettlementCard(Card card,int userIndex = -1,int targetIndex = -1)
    {
        waitSettlementCard = card;
        settlementCardAvailability = true;
        settlementCardTarget = targetIndex;
        settlementCardUser = userIndex;
    }

    public void SetWaitBurnCard(Card card)
    {
        waitBurnCard = card;
    }

    //识破
    public void Penetrate()
    {
        if (settlementCardAvailability)
        {
            settlementCardAvailability = false;
        }
        else
        {
            settlementCardAvailability = true;
        }
    }

    public void Gambling(int targetIndex)
    {
        Card gamblingCard = cardList[0];
        cardList.RemoveAt(0);
        playerArr[targetIndex].AddMessage(gamblingCard);




        GameMsg msg = new GameMsg
        {
            cmd = CMD.PushGamblingCard,
            pushGamblingCard = new PushGamblingCard
            {
                card = gamblingCard,
                index = targetIndex
            }
        };

        CacheSvc.Instance.SendMsgAll(this, msg);


        GameMsg updateMsg = new GameMsg
        {
            cmd = CMD.PushSinglePlayerMessageUpdate,
            pushSinglePlayerMessageUpdate = new PushSinglePlayerMessageUpdate
            {
                posIndex = targetIndex,
                cards = playerArr[targetIndex].cardList.Count,
                redNum = playerArr[targetIndex].redNum,
                blueNum = playerArr[targetIndex].blueNum,
                blackNum = playerArr[targetIndex].blackNum,
                cardLibraryCount = cardList.Count
            }
        };

        CacheSvc.Instance.SendMsgAll(this, updateMsg);
    }

    public void SendProbingMsg(int target,int action)
    {
        GameMsg msg = new GameMsg
        {
            cmd = CMD.PushProbingInfo,
            pushProbingInfo = new PushProbingInfo
            {
                targetIndex = target,
                responseAction = action
            }
        };
        CacheSvc.Instance.SendMsgAll(this,msg);
    }

    public void RealOrFalse()
    {
        List<Card> tempCardList = new List<Card>();

        for (int i = 0; i < playerArr.Length; i++)
        {
            playerArr[i].AddMessage(cardList[0]);
            tempCardList.Add(cardList[0]);
            cardList.RemoveAt(0);
        }

        GameMsg msg = new GameMsg
        {
            cmd = CMD.PushRealOrFalseInfo,
            pushRealOrFalseInfo = new PushRealOrFalseInfo
            {
                cardList = tempCardList,
                index = roundPlayerIndex
            }
        };

        CacheSvc.Instance.SendMsgAll(this, msg);

    }

    public void Locking(int targetIndex)
    {
        playerArr[targetIndex].playerState = PlayerState.Locking;
        GameMsg msg = new GameMsg
        {
            cmd = CMD.PushLockingInfo,
            pushLockingInfo = new PushLockingInfo
            {
                index = targetIndex
            }
        };
        CacheSvc.Instance.SendMsgAll(this,msg);
    }

    public void TigerMountain(int targetIndex)
    {
        if(playerArr[targetIndex].playerState != PlayerState.Locking)
        {
            playerArr[targetIndex].playerState = PlayerState.Transfer;
            GameMsg msg = new GameMsg
            {
                cmd = CMD.PushTigerMountainInfo,
                pushTigerMountainInfo = new PushTigerMountainInfo
                {
                    index = targetIndex
                }
            };
            CacheSvc.Instance.SendMsgAll(this, msg);
        }

    }

    public void Swap(Card card)
    {
        transferingMessage = card;
        GameMsg msg = new GameMsg
        {
            cmd = CMD.PushSwapInfo,
            pushSwapInfo = new PushSwapInfo
            {
                swapCard = card
            }
        };
        CacheSvc.Instance.SendMsgAll(this,msg);
    }

    public void Intercept()
    {
        GameMsg msg = new GameMsg
        {
            cmd = CMD.PushMessageTransfering,
            pushMessageTransfering = new PushMessageTransfering
            {
                message = transferingMessage,
                transferIndex = transferingMessageIndex,
                targetIndex = settlementCardUser

            }
        };
        CacheSvc.Instance.SendMsgAll(this, msg);

        transferingMessageIndex = settlementCardUser;
    }

    public void Transfer()
    {
        GameMsg msg = new GameMsg
        {
            cmd = CMD.PushMessageTransfering,
            pushMessageTransfering = new PushMessageTransfering
            {
                message = transferingMessage,
                transferIndex = settlementCardUser,
                targetIndex = settlementCardTarget

            }
        };
        CacheSvc.Instance.SendMsgAll(this, msg);
        Locking(settlementCardTarget);
        transferingMessageIndex = settlementCardTarget;
    }

    public void Decipher()
    {
        GameMsg msg = new GameMsg
        {
            cmd = CMD.PushDecipherInfo,
            pushDecipherInfo = new PushDecipherInfo
            {
                index = settlementCardUser
            }
        };
        CacheSvc.Instance.SendMsgAll(this, msg);

    }

    //卡牌结算
    public void CardSettlement()
    {
        GameMsg playMsg = new GameMsg();

        switch (roundStage)
        {
            case RoundStage.PlayStage:
                playMsg.cmd = CMD.PushPlayStage;
                break;
            case RoundStage.ArriveSection:
                playMsg.cmd = CMD.PushArriveSection;
                break;
            default:
                break;
        }
        if (settlementCardAvailability)
        {
            //结算卡牌

            switch (waitSettlementCard.function)
            {
                case CardFunction.ProbingLurker_0:
                    if (playerArr[settlementCardTarget].playerIdentity == PlayerIdentity.Lurker)
                    {
                        GameMsg msg = new GameMsg
                        {
                            cmd = CMD.PushDisCard,
                            pushDisCard = new PushDisCard
                            {
                                targetIndex = settlementCardTarget
                            }
                        };
                        CacheSvc.Instance.SendMsgAll(this, msg);
                        SendProbingMsg(settlementCardTarget, 0);
                    }
                    else
                    {
                        SendProbingMsg(settlementCardTarget, 2);
                        CacheSvc.Instance.SendMsgAll(this, playMsg);

                    }
                    break;
                case CardFunction.ProbingLurker_1:
                    if (playerArr[settlementCardTarget].playerIdentity == PlayerIdentity.Lurker)
                    {
                        DrawCard(settlementCardTarget, 1);
                        SendProbingMsg(settlementCardTarget, 1);
                    }
                    else
                    {
                        SendProbingMsg(settlementCardTarget, 2);
                    }
                    CacheSvc.Instance.SendMsgAll(this, playMsg);

                    break;
                case CardFunction.ProbingMilitary_0:
                    if (playerArr[settlementCardTarget].playerIdentity == PlayerIdentity.Military)
                    {
                        GameMsg msg = new GameMsg
                        {
                            cmd = CMD.PushDisCard,
                            pushDisCard = new PushDisCard
                            {
                                targetIndex = settlementCardTarget
                            }
                        };
                        CacheSvc.Instance.SendMsgAll(this, msg);
                        SendProbingMsg(settlementCardTarget, 0);
                    }
                    else
                    {
                        SendProbingMsg(settlementCardTarget, 2);
                        CacheSvc.Instance.SendMsgAll(this, playMsg);

                    }
                    break;
                case CardFunction.ProbingMilitary_1:
                    if (playerArr[settlementCardTarget].playerIdentity == PlayerIdentity.Military)
                    {
                        DrawCard(settlementCardTarget, 1);
                        SendProbingMsg(settlementCardTarget, 1);
                    }
                    else
                    {
                        SendProbingMsg(settlementCardTarget, 2);
                    }
                    CacheSvc.Instance.SendMsgAll(this, playMsg);

                    break;
                case CardFunction.ProbingSoySauce_0:
                    if (playerArr[settlementCardTarget].playerIdentity == PlayerIdentity.SoySauce)
                    {
                        GameMsg msg = new GameMsg
                        {
                            cmd = CMD.PushDisCard,
                            pushDisCard = new PushDisCard
                            {
                                targetIndex = settlementCardTarget
                            }
                        };
                        CacheSvc.Instance.SendMsgAll(this, msg);
                        SendProbingMsg(settlementCardTarget, 0);
                    }
                    else
                    {
                        SendProbingMsg(settlementCardTarget, 2);
                        CacheSvc.Instance.SendMsgAll(this, playMsg);

                    }
                    break;
                case CardFunction.ProbingSoySauce_1:
                    if (playerArr[settlementCardTarget].playerIdentity == PlayerIdentity.SoySauce)
                    {
                        DrawCard(settlementCardTarget, 1);
                        SendProbingMsg(settlementCardTarget, 1);
                    }
                    else
                    {
                        SendProbingMsg(settlementCardTarget, 2);
                    }
                    CacheSvc.Instance.SendMsgAll(this, playMsg);

                    break;
                case CardFunction.Locking:
                    Locking(settlementCardTarget);

                    CacheSvc.Instance.SendMsgAll(this, playMsg);
                    break;
                case CardFunction.TigerMountain:
                    TigerMountain(settlementCardTarget);

                    CacheSvc.Instance.SendMsgAll(this, playMsg);
                    break;
                case CardFunction.Reinforce:
                    int drawCardCount = 1;
                    drawCardCount += playerArr[roundPlayerIndex].blackNum;
                    DrawCard(roundPlayerIndex, drawCardCount);
                    CacheSvc.Instance.SendMsgAll(this, playMsg);

                    break;
                case CardFunction.Gambling:
                    Gambling(settlementCardTarget);
                    CacheSvc.Instance.SendMsgAll(this, playMsg);

                    break;
                case CardFunction.Balance:

                    GameMsg balanceMsg = new GameMsg
                    {
                        cmd = CMD.PushBalanceInfo
                    };


                    CacheSvc.Instance.SendMsgAll(this, balanceMsg);

                    break;
                case CardFunction.Burn:

                    
                    RemoveMessage(settlementCardTarget, waitBurnCard);

                    CacheSvc.Instance.SendMsgAll(this, playMsg);

                    break;
                case CardFunction.RealOrFalse:
                    RealOrFalse();

                    break;
                case CardFunction.Swap:
                    Swap(waitSettlementCard);
                    CacheSvc.Instance.SendMsgAll(this, playMsg);

                    break;
                case CardFunction.Intercept:

                    Intercept();
                    CacheSvc.Instance.SendMsgAll(this, playMsg);

                    break;
                case CardFunction.Transfer:

                    Transfer();
                    CacheSvc.Instance.SendMsgAll(this, playMsg);
                    break;
                case CardFunction.Decipher:
                    Decipher();
                    break;
                default:
                    CacheSvc.Instance.SendMsgAll(this, playMsg);

                    break;
            }
        }
        else
        {

            CacheSvc.Instance.SendMsgAll(this, playMsg);

            //无事发生
        }
        waitSettlementCard = null;
        settlementCardAvailability = false;
        settlementCardTarget = -1;
        settlementCardUser = -1;
}
}



public enum RoomState
{
    None,
    Matching,
    End
}

public enum RoundStage
{
    None,
    RoundStart,//回合开始阶段
    DrawPhase,//抽牌阶段
    PlayStage,//出牌阶段
    MessageTransfer,//情报传递阶段
    TransferSection,//传递小节
    ArriveSection,//到达小节
    AcceptSection,//接受小节
    RoundEnd //回合结束阶段
}
